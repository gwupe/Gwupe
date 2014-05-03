using System;
using System.Net;
using BlitsMe.Agent.Components.Functions.API;
using BlitsMe.Communication.P2P.P2P.Connector;
using BlitsMe.Communication.P2P.P2P.Socket;
using BlitsMe.Communication.P2P.P2P.Socket.API;
using log4net;

namespace BlitsMe.Agent.Components.Functions.RemoteDesktop
{
    internal class Server : ServerImpl
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Server));
        private StreamProxy _proxy;

#if DEBUG
        private const int VNCPort = 10231;
#else
        private const int VNCPort = 10230;
#endif

        public bool Listening
        {
            get { return Socket != null && Socket.Listening; }
        }

        public bool Established
        {
            get { return Socket != null && Socket.Connected; }
        }

        internal void Listen(String connectionId)
        {
            BlitsMeClientAppContext.CurrentAppContext.P2PManager.AwaitConnection(connectionId, ReceiveConnection);
        }

        private void ReceiveConnection(ISocket socket)
        {
            Socket = socket;
            Socket.ConnectionOpened += (sender, args) => { Closed = false; };
            Socket.ConnectionClosed += (sender, args) => Close();
            //Socket.ListenOnce();
            _proxy = new StreamProxy(Socket, new BmTcpSocket(new IPEndPoint(IPAddress.Loopback, VNCPort)));
            _proxy.Start();
        }

        // This is called by the RDP Function, so stop listening and terminate connections
        internal override void Close()
        {
            if (!Closing && !Closed)
            {
                Logger.Debug("Closing RemoteDesktop ServerProxy");
                Closing = true;
                if (_proxy != null)
                {
                    _proxy.Close();
                }
                _proxy = null;
                Closing = false;
                Closed = true;
            }
        }
    }
}
