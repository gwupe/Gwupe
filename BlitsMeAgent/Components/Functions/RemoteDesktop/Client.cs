using System;
using System.Net;
using BlitsMe.Communication.P2P.RUDP.Connector;
using BlitsMe.Communication.P2P.RUDP.Connector.API;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;
using log4net;
using log4net.Repository.Hierarchy;

namespace BlitsMe.Agent.Components.Functions.RemoteDesktop
{
    internal class Client
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (Client));
        private ProxyFromTcpConnector _connector;
        private readonly ITransportManager _transportManager;
        public bool Closing { get; private set; }
        public bool Closed { get; private set; }

        #region Event Handling

        public event EventHandler ConnectionClosed;

        protected virtual void OnConnectionClosed()
        {
            EventHandler handler = ConnectionClosed;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        public event EventHandler ConnectionAccepted;

        protected virtual void OnConnectionAccepted()
        {
            EventHandler handler = ConnectionAccepted;
            if (handler != null) handler(this, EventArgs.Empty);
        }


        private void ConnectorOnConnectionClosed(object sender, NamedConnectionEventArgs namedConnectionEventArgs)
        {
            Close();
        }

        private void ConnectorOnConnectionAccepted(object sender, NamedConnectionEventArgs namedConnectionEventArgs)
        {
            OnConnectionAccepted();
        }

        #endregion

        public IPEndPoint LocalEndPoint
        {
            get { return _connector.ListenerEndpoint; }
        }

        internal Client(ITransportManager transportManager)
        {
            _transportManager = transportManager;
            Closed = true;
        }

        internal int Start()
        {
            Closed = false;
            _connector = new ProxyFromTcpConnector("RDP", _transportManager);
            _connector.ConnectionAccepted += ConnectorOnConnectionAccepted;
            _connector.ConnectionClosed += ConnectorOnConnectionClosed;
            return _connector.ListenOnce();
        }

        internal bool Connected { get { return _connector != null && _connector.HasConnections; } }

        internal void Close()
        {
            if (!Closing && !Closed)
            {
                Logger.Debug("Closing RemoteDesktop Proxy to local service");
                Closing = true;
                if (_connector != null)
                {
                    // Close all active connections
                    _connector.CloseConnections();
                    // Close the connector itself
                    _connector.Close();
                }
                // Notify listeners that we are done
                OnConnectionClosed();
                Closed = true;
                Closing = false;
                _connector = null;
            }
        }

    }
}
