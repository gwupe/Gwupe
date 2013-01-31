using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlitsMe.Communication.P2P.Exceptions;
using BlitsMe.Communication.P2P.RUDP.Connector.API;
using BlitsMe.Communication.P2P.RUDP.Socket.API;
using System.Net;
using System.Net.Sockets;
using log4net;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;

namespace BlitsMe.Communication.P2P.RUDP.Connector
{
    public class ProxyTcpListener : INamedListener
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ProxyTcpListener));
        public bool Listening { get; private set; }
        public bool HasConnections { get { return _openConnections != null && _openConnections.Count > 0; } }
        public String Name { get; private set; }
        public IPEndPoint ProxyEndPoint { get; private set; }
        private readonly ITransportManager _transportManager;
        private readonly List<ProxyConnection> _openConnections;

        public ProxyTcpListener(String name, IPEndPoint endPoint, ITransportManager transportManager)
        {
            _transportManager = transportManager;
            Name = name;
            ProxyEndPoint = endPoint;
            _openConnections = new List<ProxyConnection>();
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

        #endregion

        #region Connection Processing

        private bool ProcessConnect(ITcpOverUdptSocket socket)
        {
            try
            {
                // Attempt to connect end point
                TcpClient client = new TcpClient();
                client.Connect(ProxyEndPoint);
                OnConnectionAccepted();
                // Setup the proxying threads
                ProxyConnection proxyConnection = new ProxyConnection(client, socket);
                proxyConnection.Closed += ProxyConnectionOnClosed;
                proxyConnection.Start();
                _openConnections.Add(proxyConnection);

            }
            catch (Exception e)
            {
                Logger.Error(
                    "Failed to connect to proxy endpoint [" + ProxyEndPoint + "], cannot proxy : " + e.Message, e);
                return false;
            }

            return true;
        }

        private bool ProcessOneConnect(ITcpOverUdptSocket socket)
        {
            StopListening();
            return ProcessConnect(socket);
        }

        private void ProxyConnectionOnClosed(object sender, EventArgs eventArgs)
        {
            var proxyConnect = sender as ProxyConnection;
            if (_openConnections.Contains(proxyConnect))
            {
                _openConnections.Remove(proxyConnect);
            }
            OnConnectionClosed();
        }

        #endregion

        #region Listening

        public void Listen()
        {
            if (!Listening)
            {
                _transportManager.TCPTransport.Listen(this.Name, ProcessConnect);
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
                _transportManager.TCPTransport.Listen(Name, ProcessOneConnect);
                Listening = true;
            }
            else
            {
                throw new ConnectionException("Cannot listen, already listening for " + Name);
            }
        }

        private void StopListening()
        {
            if (Listening)
            {
#if DEBUG
                Logger.Debug("Stopping listening for connections from connection named " + Name + " for " + ProxyEndPoint);
#endif
                this.Listening = false;
                _transportManager.TCPTransport.StopListen(Name);
            }
        }

        #endregion

        public void Close()
        {
            StopListening();
#if DEBUG
            Logger.Debug("Stopping all active connections from named connection " + Name + " for " + ProxyEndPoint);
#endif
            foreach (ProxyConnection openConnection in _openConnections)
            {
                openConnection.Close();
            }
            _openConnections.Clear();

        }


    }
}
