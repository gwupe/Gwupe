using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using BlitsMe.Communication.P2P.RUDP.Socket.API;

namespace BlitsMe.Communication.P2P.RUDP.Connector.API
{
    public interface INamedListener
    {
        // Connected
        event EventHandler<NamedConnectionEventArgs> ConnectionAccepted;
        // Disconnected
        event EventHandler<NamedConnectionEventArgs> ConnectionClosed;
        // Are we listening?
        bool Listening { get; }
        // The name to listen on
        String Name { get; }
        // The listen method
        void Listen();
        // The listen method
        void ListenOnce();
        // The close method
        void Close();
    }

    public class NamedConnectionEventArgs : EventArgs
    {
        public IPAddress RemoteIp;
        public bool Connected;
        public String Name;
        private bool _tcp;
        public bool Tcp
        {
            get { return _tcp; }
            set { _tcp = value; }
        }
        public bool Udp
        {
            get { return !_tcp; }
            set { _tcp = !value; }
        }
    }
}
