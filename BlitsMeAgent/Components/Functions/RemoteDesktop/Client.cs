using System;
using System.Net;
using BlitsMe.Communication.P2P.RUDP.Connector;
using BlitsMe.Communication.P2P.RUDP.Connector.API;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;

namespace BlitsMe.Agent.Components.Functions.RemoteDesktop
{
    internal class Client
    {
        private ProxyTcpConnector _connector;
        private readonly ITransportManager _transportManager;

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
            OnConnectionClosed();
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
        }

        internal int Start()
        {
            _connector = new ProxyTcpConnector("RDP", _transportManager);
            _connector.ConnectionAccepted += ConnectorOnConnectionAccepted;
            _connector.ConnectionClosed += ConnectorOnConnectionClosed;
            return _connector.ListenOnce();
        }

        internal bool Connected { get { return _connector != null && _connector.HasConnections; } }

        internal void Close()
        {
            if(Connected)
            {
                _connector.Close();
            }
        }

    }
}
