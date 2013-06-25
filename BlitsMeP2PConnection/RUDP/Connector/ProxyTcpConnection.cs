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

        private readonly Thread _proxyReverseThread;
        private readonly TcpClient _tcpClient;

        internal ProxyTcpConnection(TcpClient client, ITcpOverUdptSocket socket)
            : base(socket)
        {
            _tcpClient = client;
            _proxyReverseThread = new Thread(ProxyTransportWriter) { IsBackground = true };
            _proxyReverseThread.Name = "_proxyReverseThread[" + _proxyReverseThread.ManagedThreadId + "]";
        }

        protected override void _Start()
        {
            _proxyReverseThread.Start();
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
                _tcpClient.Close();
            }
            if (_proxyReverseThread.IsAlive && !_proxyReverseThread.Equals(Thread.CurrentThread))
            {
#if(DEBUG)
                Logger.Debug("Shutting of reverse proxy thread.");
#endif
                _proxyReverseThread.Abort();
            }
        }

        protected override bool ProcessTransportRead(byte[] read)
        {
            if (_tcpClient.Connected)
            {

                _tcpClient.GetStream().Write(read, 0, read.Length);
#if(DEBUG)
                Logger.Debug("Wrote " + read.Length + " to tcp socket.");
#endif
            }
            else
            {
#if(DEBUG)
                Logger.Debug("Outgoing connection has been closed, stopping forward proxy");
#endif
                return false;
            }
            return true;
        }

        private void ProxyTransportWriter()
        {
            try
            {
                int read = -1;
                while (read != 0)
                {
                    byte[] tmpRead = new byte[16384];
                    read = _tcpClient.GetStream().Read(tmpRead, 0, tmpRead.Length);
                    if (read > 0)
                    {
#if(DEBUG)
                        Logger.Debug("Read " + read + " bytes from tcp socket, writing to transportManager [md5=" + Util.getSingleton().getMD5Hash(tmpRead, 0, read) + "]");
#endif
                        try
                        {
                            if (!SendDataToTransport(tmpRead, read)) break;
                        }
                        catch (Exception e)
                        {
                            Logger.Error("Sending to transport failed, shutting down ProxyTransportWriter");
                            break;
                        }
                    }
#if(DEBUG)
                    else
                    {
                        Logger.Debug("Incoming tcp stream has closed");
                    }
#endif
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Connection has failed : " + ex.Message);
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