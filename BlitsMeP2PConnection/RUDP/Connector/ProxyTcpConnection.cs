using System;
using System.Net.Sockets;
using System.Threading;
using BlitsMe.Common.Security;
using BlitsMe.Communication.P2P.RUDP.Connector.API;
using BlitsMe.Communication.P2P.RUDP.Socket.API;
using log4net;

namespace BlitsMe.Communication.P2P.RUDP.Connector
{
    internal class ProxyTcpConnection : TcpTransportConnection
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ProxyTcpConnection));

        private readonly Thread _tcpClientProxyReader;
        private readonly TcpClient _tcpClient;

        internal ProxyTcpConnection(TcpClient client, ITcpOverUdptSocket socket)
            : base(socket)
        {
            _tcpClient = client;
            _tcpClientProxyReader = new Thread(TcpReaderToTransportSocketWriter) { IsBackground = true };
            _tcpClientProxyReader.Name = "_tcpReaderToTransportSockWriter[" + socket.Connection.ConnectionId + "]";
            _tcpClientProxyReader.Start();
            // start the base class processes
            CompleteInit();
        }

        protected override void _Close()
        {
            Logger.Debug(_tcpClient);
            Logger.Debug(_tcpClient.GetStream());
            if (_tcpClient.Connected)
            {
                Logger.Debug("Closing TCP Client");
                _tcpClient.GetStream().Close();
                _tcpClient.Close();
            }
        }

        protected override bool ProcessTransportSocketRead(byte[] read, int length)
        {
            if (_tcpClient != null)
            {
                if (_tcpClient.Connected)
                {
                    _tcpClient.GetStream().Write(read, 0, length);
#if(DEBUG)
                Logger.Debug("Wrote " + length + " to tcp socket.");
#endif
                }
                else
                {
                    Logger.Debug("Outgoing connection has been closed");
                    return false;
                }
            } else
            {
                Logger.Error("TcpClient is null, this should not be possible");
                return false;
            }
            return true;
        }

        private void TcpReaderToTransportSocketWriter()
        {
            try
            {
                int read = -1;
                while (read != 0)
                {
                    byte[] tmpRead = new byte[16384];
                    try
                    {
                        read = _tcpClient.GetStream().Read(tmpRead, 0, tmpRead.Length);
                    }
                    catch (Exception e)
                    {
                        Logger.Debug("Reading from the tcp stream caused an exception, most likely its gone.", e);
                        read = 0;
                    }
                    if (read > 0)
                    {
#if(DEBUG)
                        Logger.Debug("Read " + read + " bytes from tcp socket, writing to transportManager [md5=" + Util.getSingleton().getMD5Hash(tmpRead, 0, read) + "]");
#endif
                        try
                        {
                            if (!SendDataToTransportSocket(tmpRead, read))
                            {
                                Logger.Debug("Failed to read from the transport socket, closing.");
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Error("Sending to transport failed, shutting down ProxyTransportWriter", e);
                            break;
                        }
                    }
                    else
                    {
                        Logger.Info("Proxied tcp stream has closed");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Connection has failed : " + ex.Message, ex);
            }
            finally
            {
                try
                {
                    Close();
                }
                catch (Exception e)
                {
                    Logger.Error("Error while closing ProxyConnection : " + e.Message, e);
                }
            }
        }

    }
}