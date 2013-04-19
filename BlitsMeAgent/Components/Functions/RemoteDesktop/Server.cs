using System;
using System.Net;
using BlitsMe.Communication.P2P.RUDP.Connector;
using BlitsMe.Communication.P2P.RUDP.Connector.API;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;

namespace BlitsMe.Agent.Components.Functions.RemoteDesktop
{
    internal class Server
    {
        private ProxyTcpTransportListener _vncListener;
        private readonly ITransportManager _transportManager;
#if DEBUG
        private const int VNCPort = 10231;
#else
        private const int VNCPort = 10230;
#endif
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


        private void VNCListenerOnConnectionClosed(object sender, NamedConnectionEventArgs namedConnectionEventArgs)
        {
            OnConnectionClosed();
        }

        private void VNCListenerOnConnectionAccepted(object sender, NamedConnectionEventArgs namedConnectionEventArgs)
        {
            OnConnectionAccepted();
        }

        #endregion

        internal Server(ITransportManager manager)
        {
            _transportManager = manager;
        }

        public bool Started
        {
            get { return _vncListener != null; }
        }

        internal void Start()
        {
            _vncListener = new ProxyTcpTransportListener("RDP", new IPEndPoint(IPAddress.Loopback, VNCPort), _transportManager);
            _vncListener.ConnectionAccepted += VNCListenerOnConnectionAccepted;
            _vncListener.ConnectionClosed += VNCListenerOnConnectionClosed;
            _vncListener.ListenOnce();
        }


        internal void Close()
        {
            if (Started)
            {
                _vncListener.Close();
            }
        }
    }
}
