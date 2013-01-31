using System;
using System.Net.Sockets;
using System.Threading;
using BlitsMe.Common.Security;
using BlitsMe.Communication.P2P.RUDP.Socket.API;
using log4net;

namespace BlitsMe.Communication.P2P.RUDP.Connector
{
    internal class ProxyConnection
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ProxyConnection));


        private readonly Thread _proxyForwardThread;
        private readonly Thread _proxyReverseThread;

        private readonly TcpClient _tcpClient;
        private readonly ITcpOverUdptSocket _socket;

        internal bool Started { get; private set; }
        internal event EventHandler Closed;

        private void OnClosed()
        {
            EventHandler handler = Closed;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        internal ProxyConnection(TcpClient client, ITcpOverUdptSocket socket)
        {
            _tcpClient = client;
            _socket = socket;
            _proxyForwardThread = new Thread(ProxyTrafficForward) { IsBackground = true };
            _proxyForwardThread.Name = "_proxyForwardThread[" + _proxyForwardThread.ManagedThreadId + "]";
            _proxyReverseThread = new Thread(ProxyTrafficReverse) { IsBackground = true };
            _proxyReverseThread.Name = "_proxyReverseThread[" + _proxyReverseThread.ManagedThreadId + "]";
        }

        internal void Start()
        {
            _proxyForwardThread.Start();
            _proxyReverseThread.Start();
            Started = true;
        }


        internal void Close()
        {
            Close(false);
        }

        internal void Close(bool initiatedBySelf)
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
            if (!_socket.Closed)
            {
#if(DEBUG)
                Logger.Debug("Closing Socket");
#endif
                _socket.Close();
            }
            if (_proxyForwardThread.IsAlive && !_proxyForwardThread.Equals(Thread.CurrentThread))
            {
#if(DEBUG)
                Logger.Debug("Shutting of forward proxy thread.");
#endif
                _proxyForwardThread.Abort();
            }
            if (_proxyReverseThread.IsAlive && !_proxyReverseThread.Equals(Thread.CurrentThread))
            {
#if(DEBUG)
                Logger.Debug("Shutting of reverse proxy thread.");
#endif
                _proxyReverseThread.Abort();
            }
            if (Started)
            {
                Started = false;
                OnClosed();
            }
        }

        private void ProxyTrafficForward()
        {
            try
            {
                int read = -1;
                while (read != 0)
                {
                    byte[] tmpRead = new byte[8192];
                    read = _socket.Read(tmpRead, tmpRead.Length);
                    if (read > 0)
                    {
#if(DEBUG)
                        Logger.Debug("Read " + read + " bytes from transportManager, writing to tcp socket [md5=" + Util.getSingleton().getMD5Hash(tmpRead, 0, read) + "]");
#endif
                        if (_tcpClient.Connected)
                        {
                            _tcpClient.GetStream().Write(tmpRead, 0, read);
                        }
                        else
                        {
#if(DEBUG)
                            Logger.Debug("Outgoing connection has been closed, stopping forward proxy");
#endif
                            break;
                        }
                    }
                    else
                    {
#if(DEBUG)
                        Logger.Error("Incoming transportManager socket has closed");
#endif
                    }
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
                    this.Close(true);
                }
                catch (Exception e)
                {
                    Logger.Error("Error while closing ProxyConnection : " + e.Message, e);
                }
            }
        }

        private void ProxyTrafficReverse()
        {
            try
            {
                int read = -1;
                while (read != 0)
                {
                    byte[] tmpRead = new byte[8192];
                    read = _tcpClient.GetStream().Read(tmpRead, 0, tmpRead.Length);
                    if (read > 0)
                    {
#if(DEBUG)
                        Logger.Debug("Read " + read + " bytes from tcp socket, writing to transportManager [md5=" + Util.getSingleton().getMD5Hash(tmpRead, 0, read) + "]");
#endif
                        byte[] write = new byte[read];
                        Array.Copy(tmpRead, 0, write, 0, read);
                        if (!_socket.Closed)
                        {
                            _socket.Send(write, 30000);
                        }
                        else
                        {
#if(DEBUG)
                            Logger.Debug("Outgoing transportManager socket has been closed, stopping forward proxy");
#endif
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
                    this.Close(true);
                }
                catch (Exception e)
                {
                    Logger.Error("Error while closing ProxyConnection : " + e.Message, e);
                }
            }
        }
    }
}