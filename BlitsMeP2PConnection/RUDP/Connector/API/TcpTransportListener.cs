using System;
using System.Collections.Generic;
using BlitsMe.Communication.P2P.Exceptions;
using BlitsMe.Communication.P2P.RUDP.Socket.API;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;
using log4net;

namespace BlitsMe.Communication.P2P.RUDP.Connector.API
{
    //public delegate TcpTransportConnection ProcessConnect(ITcpOverUdptSocket socket);

    public abstract class TcpTransportListener : INamedListener
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TcpTransportListener));
        public bool Listening { get; private set; }
        public bool HasConnections { get { return _openConnections != null && _openConnections.Count > 0; } }
        public String Name { get; private set; }
        private readonly ITransportManager _transportManager;
        private readonly List<TcpTransportConnection> _openConnections;
        public bool Closing { get; private set; }

        public TcpTransportListener(String name, ITransportManager transportManager)
        {
            _transportManager = transportManager;
            Name = name;
            _openConnections = new List<TcpTransportConnection>();
        }

        #region Event Handling

        public event EventHandler<NamedConnectionEventArgs> ConnectionAccepted;

        protected virtual void OnConnectionAccepted()
        {
            EventHandler<NamedConnectionEventArgs> handler = ConnectionAccepted;
            if (handler != null)
            {
                var args = new NamedConnectionEventArgs()
                    {
                        Connected = true,
                        Name = Name,
                        RemoteIp = _transportManager.RemoteIp,
                        Tcp = true
                    };
                handler(this, args);
            }
        }

        public event EventHandler<NamedConnectionEventArgs> ConnectionClosed;

        protected virtual void OnConnectionClosed()
        {
            EventHandler<NamedConnectionEventArgs> handler = ConnectionClosed;
            if (handler != null)
            {
                var args = new NamedConnectionEventArgs()
                    {
                        Connected = false,
                        Name = Name,
                        RemoteIp = _transportManager.RemoteIp,
                        Tcp = true
                    };
                handler(this, args);
            }
        }

        public void Close()
        {
            if (!Closing)
            {
                Closing = true;
                StopListening();
                CloseConnections();
            }
        }

        private void CloseConnections()
        {
#if DEBUG
            Logger.Debug("Stopping all active connections from named connection " + Name);
#endif
            // This is necessary because of the event which will try remove the conn from the list (here it won't find it)
            while(_openConnections != null && _openConnections.Count > 0)
            {
                var conn = _openConnections[0];
                _openConnections.Remove(conn);
                conn.Close();
            }
        }

        #endregion

        #region ConnectionProcessing

        protected abstract TcpTransportConnection ProcessConnect(ITcpOverUdptSocket socket);

        private bool ProcessConnection(ITcpOverUdptSocket arg)
        {
            var tcpConnection = ProcessConnect(arg);
            if (tcpConnection == null) return false;
            // Fire connection accepted event
            OnConnectionAccepted();
            // Make sure we know when it has closed
            tcpConnection.CloseConnection += TcpConnectionOnCloseConnection;
            _openConnections.Add(tcpConnection);
            return true;
        }

        private void TcpConnectionOnCloseConnection(object sender, EventArgs eventArgs)
        {
            var proxyConnect = sender as TcpTransportConnection;
            if (_openConnections.Contains(proxyConnect))
            {
                _openConnections.Remove(proxyConnect);
            }
            OnConnectionClosed();
        }

        private bool ProcessOneConnection(ITcpOverUdptSocket socket)
        {
            StopListening();
            return ProcessConnection(socket);
        }

        #endregion

        #region Listening

        public void Listen()
        {
            if (!Listening)
            {
                _transportManager.TCPTransport.Listen(this.Name, ProcessConnection);
                Listening = true;
            }
            else
            {
                throw new ConnectionException("Cannot listen, already listening for " + Name);
            }
        }

        public void ListenOnce()
        {
            if (!Listening)
            {
                _transportManager.TCPTransport.Listen(Name, ProcessOneConnection);
                Listening = true;
            }
            else
            {
                throw new ConnectionException("Cannot listen, already listening for " + Name);
            }
        }

        public void StopListening()
        {
            if (Listening)
            {
#if DEBUG
                Logger.Debug("Stopping listening for connections from connection named " + Name);
#endif
                this.Listening = false;
                _transportManager.TCPTransport.StopListen(Name);
            }
        }

        #endregion

    }
}
