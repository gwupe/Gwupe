using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gwupe.Agent.Components.Functions.RemoteDesktop;
using Gwupe.Agent.Components.Person;
using Gwupe.Communication.P2P.P2P.Socket.API;
using log4net;

namespace Gwupe.Agent.Components.Functions.API
{
    internal abstract class ClientImpl
    {
        protected readonly Attendance SecondParty;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ClientImpl));
        protected  ISocket Socket;
        private bool _closed = true;

        public bool Closing { get; protected set; }

        public bool Closed
        {
            get { return _closed; }
            protected set
            {
                if (value && !_closed)
                {
                    _closed = true; OnClientConnectionClosed();
                } else if (!value && _closed)
                {
                    _closed = false; OnClientConnectionOpened();
                }
            }
        }

        protected ClientImpl(Attendance secondParty)
        {
            SecondParty = secondParty;
        }

        #region Event Handling

        internal event EventHandler ClientConnectionClosed;

        protected virtual void OnClientConnectionClosed()
        {
            EventHandler handler = ClientConnectionClosed;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        internal event EventHandler ClientConnectionOpened;

        protected virtual void OnClientConnectionOpened()
        {
            EventHandler handler = ClientConnectionOpened;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        #endregion

        internal abstract void Close();
    }
}
