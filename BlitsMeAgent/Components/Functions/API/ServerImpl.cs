using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlitsMe.Communication.P2P.P2P.Socket.API;

namespace BlitsMe.Agent.Components.Functions.API
{
    internal abstract class ServerImpl
    {
        protected ISocket Socket;
        private bool _closed = true;

        public bool Closing { get; protected set; }

        public bool Closed
        {
            get { return _closed; }
            protected set
            {
                if (value && !_closed)
                {
                    _closed = true; OnServerConnectionClosed();
                }
                else if (!value && _closed)
                {
                    _closed = false; OnServerConnectionOpened();
                }
            }
        }

        #region Event Handling

        internal event EventHandler ServerConnectionClosed;

        protected virtual void OnServerConnectionClosed()
        {
            EventHandler handler = ServerConnectionClosed;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        internal event EventHandler ServerConnectionOpened;

        protected virtual void OnServerConnectionOpened()
        {
            EventHandler handler = ServerConnectionOpened;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        #endregion

        internal abstract void Close();
    }
}
