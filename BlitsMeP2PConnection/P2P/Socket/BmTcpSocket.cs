using System;
using System.Net;
using System.Net.Sockets;
using BlitsMe.Communication.P2P.P2P.Socket.API;
using log4net;
using log4net.Repository.Hierarchy;

namespace BlitsMe.Communication.P2P.P2P.Socket
{
    public class BmTcpSocket : ISocket
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (BmTcpSocket));
        private readonly IPEndPoint _bindLocalEndPoint;
        private String _toString;
        private TcpListener _listener;
        private TcpClient _client;
        public IPEndPoint RemoteEndPoint
        {
            get { return (IPEndPoint)(_client == null ? null : _client.Client.RemoteEndPoint); }
        }

        public IPEndPoint LocalEndPoint
        {
            get { return (IPEndPoint)(_client == null ? _listener.LocalEndpoint : _client.Client.LocalEndPoint); }
        }

        private bool _closed;
        private bool _connected;

        public BmTcpSocket(IPEndPoint bindLocalEndPoint)
        {
            _bindLocalEndPoint = bindLocalEndPoint;
            _toString = "BmTcpSocket()";
        }

        public IPEndPoint BindListen()
        {
            _listener = new TcpListener(_bindLocalEndPoint);
            _listener.Start();
            return (IPEndPoint)_listener.LocalEndpoint;
        }

        public void ListenOnce()
        {
            try
            {
                if (!Connected)
                {
                    if (_listener == null) { BindListen(); }
                    Listening = true;
                    _toString = "BmTcpSocket(Listen " + _listener.LocalEndpoint + ")";
                    Logger.Debug("Waiting for tcp connection " + this);
                    _client = _listener.AcceptTcpClient();
                    Connected = true;
                    _listener.Stop();
                    Listening = false;
                    _toString = "BmTcpSocket(Connected " + ((IPEndPoint)_client.Client.RemoteEndPoint).Address + ":" +
                                ((IPEndPoint)_client.Client.RemoteEndPoint).Port + ")";
                    Logger.Debug("Connected " + this);
                }
            }
            finally
            {
                Listening = false;
            }
        }

        public void Connect()
        {
            if (!Connected)
            {
                _client = new TcpClient();
                _client.Connect(_bindLocalEndPoint);
                Connected = true;
                _toString = "BmTcpSocket(Connected " + ((IPEndPoint)_client.Client.RemoteEndPoint).Address + ":" +
                            ((IPEndPoint)_client.Client.RemoteEndPoint).Port + ")";
            }
        }

        public void Send(byte[] data, int length)
        {
            _client.GetStream().Write(data, 0, length);
            BufferedData += length;
        }

        public int Read(byte[] data, int maxRead)
        {
            return _client.GetStream().Read(data, 0, maxRead);
        }

        public int SendTimeout
        {
            get { return _client.SendTimeout; }
            set { _client.SendTimeout = value > 0 ? value : -1; }
        }

        public int ReadTimeout
        {
            get { return _client.ReceiveTimeout; }
            set { _client.ReceiveTimeout = value > 0 ? value : -1; }
        }

        public void Close()
        {
            if (!Closed && !Closing)
            {
                Closing = true;
                if (_client != null)
                {
                    _client.Close();
                }
                Closing = false;
                Closed = true;
            }
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

        public override String ToString()
        {
            return _toString;
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

        public int SentData { get { return BufferedData; } }

        public int BufferedData { get; private set; }

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
