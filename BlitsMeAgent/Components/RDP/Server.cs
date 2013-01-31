using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using BlitsMe.Communication.P2P.RUDP.Connector;
using BlitsMe.Communication.P2P.RUDP.Connector.API;
using BlitsMe.Communication.P2P.RUDP.Tunnel;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;

namespace BlitsMe.Agent.Components.RDP
{
    internal class Server
    {
        private ProxyTcpListener _vncListener;
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
            get { return _vncListener != null && _vncListener.Listening; }
        }

        internal void Start()
        {
            _vncListener = new ProxyTcpListener("RDP", new IPEndPoint(IPAddress.Loopback, 5900), _transportManager);
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
