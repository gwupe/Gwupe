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
        }

        protected override void _Close()
        {
#if(DEBUG)
            Logger.Debug("Closing proxied connection components");
#endif
            if (_tcpClient.Connected)
            {
#if(DEBUG)
                Logger.Debug("Closing TCP Client");
#endif
                _tcpClient.GetStream().Close();
                _tcpClient.Close();
            }
        }

        protected override bool ProcessTransportSocketRead(byte[] read, int length)
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
#if(DEBUG)
                Logger.Debug("Outgoing connection has been closed");
#endif
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
                    } catch(Exception e)
                    {
#if DEBUG
                        Logger.Debug("Reading from the tcp stream caused an exception, most likely its gone.",e);
#endif
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
#if DEBUG
                                Logger.Debug("Failed to read from the transport socket, closing.");
#endif
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Error("Sending to transport failed, shutting down ProxyTransportWriter",e);
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
                Logger.Error("Connection has failed : " + ex.Message,ex);
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