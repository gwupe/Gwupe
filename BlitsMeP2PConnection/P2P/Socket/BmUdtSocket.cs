using System;
using System.Net;
using System.Net.Sockets;
using BlitsMe.Communication.P2P.P2P.Socket.API;
using BlitsMe.Communication.P2P.P2P.Tunnel;
using log4net;

namespace BlitsMe.Communication.P2P.P2P.Socket
{
    public delegate byte[] EncryptData(byte[] data, int length);
    public delegate byte[] DecryptData(byte[] data, int length);

    public class BmUdtSocket : ITunnelEndpoint
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(BmUdtSocket));
        private const byte PACKET_CONNECTION_RQ_TYPE = 9;
        protected Udt.Socket UdtConnection;
        private readonly String _toString;

        private bool _closed;
        private bool _connected;
        private UdpClient _udpClient;
        private PeerInfo _self;

        public BmUdtSocket()
        {
            _udpClient = new UdpClient(new IPEndPoint(IPAddress.Any,0));

            Closed = false;
            Closing = false;
            //_toString = "BmUdtSocket(" + Socket.RemoteEndPoint.Address + ":" + Socket.RemoteEndPoint.Port + ")";
        }

        public PeerInfo Wave(IPEndPoint facilitator)
        {
            var waver = new Waver();
            _self = waver.Wave(facilitator, 10000, _udpClient);
            Logger.Debug("After wave, local endpoints are " + _self);
            if (_self.ExternalEndPoint == null)
            {
                Logger.Warn("Failed to get external endpoint : " + _self);
            }
            if (_self.EndPoints.Count == 0)
            {
                throw new Exception("Failed to determine any local endpoints : " + _self.ToString());
            }
            return _self;
        }

        public IPEndPoint WaitForSync(PeerInfo peer, String syncId)
        {
            var syncer = new Syncer(syncId);
            var activeIp = syncer.WaitForSyncFromPeer(peer, 10000, _udpClient);
            var udtSocket = new Udt.Socket(AddressFamily.InterNetwork, SocketType.Stream);
            udtSocket.Bind(_udpClient.Client);
            udtSocket.Listen(10);
            Udt.Socket udtClient = udtSocket.Accept();
            UdtConnection = udtClient;
            UdtConnection.BlockingReceive = true;
            Logger.Info("Successfully completed incoming tunnel with " + activeIp + "-" + syncId);
            return activeIp;
        }

        public IPEndPoint Sync(PeerInfo peer, string syncId)
        {
            var syncer = new Syncer(syncId);
            var activeIp = syncer.SyncWithPeer(peer, 10000, _udpClient);
            var udtSocket = new Udt.Socket(AddressFamily.InterNetwork, SocketType.Stream);
            udtSocket.Bind(_udpClient.Client);
            udtSocket.Connect(activeIp.Address, activeIp.Port);
            UdtConnection = udtSocket;
            UdtConnection.BlockingReceive = true;
            Logger.Info("Successfully completed outgoing tunnel with " + activeIp + "-" + syncId);
            return activeIp;
        }

        public void ListenOnce()
        {
            byte[] readBuffer = new byte[1];
            // Udt connections are already established because we need the udt ping to keep the tunnel alive, therefore we need a new "listen"
            Listening = true;
            int read = UdtConnection.Receive(readBuffer, 0, 1);
            if (read == 1 && readBuffer[0] == PACKET_CONNECTION_RQ_TYPE)
            {
                Logger.Debug("Received a connection from " + UdtConnection.RemoteEndPoint.Address);
            }
            else
            {
                throw new Exception("Failed to get a connection");
            }
            Connected = true;
            Listening = false;
        }

        public void Connect()
        {
            UdtConnection.Send(new byte[] { PACKET_CONNECTION_RQ_TYPE }, 0, 1);
            Connected = true;
        }

        public void Send(byte[] data, int length)
        {

#if DEBUG
            //Logger.Debug("Writing " + length + " bytes to the udt stream");
#endif
            UdtConnection.Send(data, 0, length);
        }

        public int Read(byte[] data, int maxRead)
        {
            return UdtConnection.Receive(data, 0, maxRead);
        }

        public int SendTimeout
        {
            get { return UdtConnection.SendTimeout; }
            set { UdtConnection.SendTimeout = value > 0 ? value : -1; }
        }

        public int ReadTimeout
        {
            get { return UdtConnection.ReceiveTimeout; }
            set { UdtConnection.ReceiveTimeout = value > 0 ? value : -1; }
        }

        public IPEndPoint RemoteEndPoint
        {
            get { return UdtConnection.RemoteEndPoint; }
        }
        public IPEndPoint LocalEndPoint
        {
            get { return UdtConnection.LocalEndPoint; }
        }

        public void Close()
        {
            if (!Closed && !Closing)
            {
                Closing = true;
                UdtConnection.Close();
                Closing = false;
                Closed = true;
                Connected = false;
            }
        }


        public override String ToString()
        {
            return _toString;
        }

        public bool Connected
        {
            get { return _connected; }
            private set
            {
                if (value)
                {
                    OnConnectionOpened();
                }
                _connected = value;
            }
        }
        public bool Closed
        {
            get { return _closed; }
            private set
            {
                if (value)
                {
                    OnConnectionClosed();
                }
                _closed = value;
            }
        }
        public bool Closing { get; private set; }

        public bool Listening { get; private set; }

        public event EventHandler ConnectionOpened;

        protected virtual void OnConnectionOpened()
        {
            EventHandler handler = ConnectionOpened;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        public event EventHandler ConnectionClosed;

        protected virtual void OnConnectionClosed()
        {
            EventHandler handler = ConnectionClosed;
            if (handler != null) handler(this, EventArgs.Empty);
        }
    }
}
