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
        internal bool Closing { get; private set; }
        internal bool Closed { get; private set; }
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
            Close();
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

        public bool Listening
        {
            get { return _vncListener != null && _vncListener.Listening; }
        }

        public bool Established
        {
            get { return _vncListener != null && _vncListener.HasConnections; }
        }

        internal void Listen()
        {
            Closed = false;
            // A proxy transport listener listens on the TM @ the named port, if it receives a connection there, it forwards all traffic to the
            // IP endpoint specified in the constructor
            _vncListener = new ProxyTcpTransportListener("RDP", new IPEndPoint(IPAddress.Loopback, VNCPort), _transportManager);
            _vncListener.ConnectionAccepted += VNCListenerOnConnectionAccepted;
            _vncListener.ConnectionClosed += VNCListenerOnConnectionClosed;
            _vncListener.ListenOnce();
        }

        // This is called by the RDP Function, so stop listening and terminate connections
        internal void Close()
        {
            if (!Closing && !Closed)
            {
                Closing = true;
                if (_vncListener != null)
                {
                    _vncListener.Close();
                }
                OnConnectionClosed();
                _vncListener = null;
                Closed = true;
                Closing = false;
            }
        }
    }
}
