using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using BlitsMe.Communication.P2P.Exceptions;
using BlitsMe.Communication.P2P.RUDP.Connector.API;
using log4net;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;
using BlitsMe.Communication.P2P.RUDP.Socket.API;

namespace BlitsMe.Communication.P2P.RUDP.Connector
{
    public class ProxyFromTcpConnector : API.INamedConnector
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ProxyFromTcpConnector));
        private TcpListener _listener;
        private Thread _listenThread;
        private readonly ITransportManager _transportManager;
        public bool Listening { get; private set; }
        public bool HasConnections { get { return _openConnections != null && _openConnections.Count > 0; } }
        public IPEndPoint ListenerEndpoint
        {
            get { return (IPEndPoint)_listener.LocalEndpoint; }
        }
        private readonly List<ProxyTcpConnection> _openConnections;
        public string Name { get; private set; }

        #region Event Handling

        public event EventHandler<NamedConnectionEventArgs> ConnectionAccepted;

        protected virtual void OnConnectionAccepted()
        {

            EventHandler<NamedConnectionEventArgs> handler = ConnectionAccepted;
            if (handler != null)
            {
                var args = new NamedConnectionEventArgs()
                {
                    Connected = true,
                    Name = Name,
                    RemoteIp = _transportManager.RemoteIp,
                    Tcp = true
                };
                handler(this, args);
            }
        }

        public event EventHandler<NamedConnectionEventArgs> ConnectionClosed;

        protected virtual void OnConnectionClosed()
        {
            EventHandler<NamedConnectionEventArgs> handler = ConnectionClosed;
            if (handler != null)
            {
                var args = new NamedConnectionEventArgs()
                {
                    Connected = false,
                    Name = Name,
                    RemoteIp = _transportManager.RemoteIp,
                    Tcp = true
                };
                handler(this, args);
            }
        }

        #endregion

        public ProxyFromTcpConnector(String name, ITransportManager transportManager)
        {
            this.Name = name;
            this._transportManager = transportManager;
            _openConnections = new List<ProxyTcpConnection>();
        }

        #region Normal TCP Listening Functionality

        public int ListenOnce()
        {
            return StartListener(0, ProcessIncomingConnection);
        }

        public int ListenOnce(int port)
        {
            return StartListener(port, ProcessIncomingConnection);
        }

        public int Listen()
        {
            return StartListener(0, ProcessIncomingConnections);
        }

        public int Listen(int port)
        {
            return StartListener(port, ProcessIncomingConnections);
        }

        private int StartListener(int port, Action listenerMethod)
        {
            if (!Listening)
            {
                _listener = new TcpListener(IPAddress.Loopback, port);
                _listener.Start();
                Listening = true;
                Logger.Debug("Started listening TCP connector on port " + ListenerEndpoint.Port + " for connections");
                _listenThread = new Thread(new ThreadStart(listenerMethod)) { IsBackground = true, Name = "_realTCPlistenThread[" + Name + "]" };
                _listenThread.Start();
                return ((IPEndPoint)_listener.LocalEndpoint).Port;
            }
            else
            {
                throw new ConnectionException("Failed to listen on port " + 0 + ", this proxy is already listening");
            }
        }


        public void StopListening()
        {
            if (Listening)
            {
                Logger.Debug("Stopping listening on TCP for incoming connections to proxy through connection named " + Name);
                this.Listening = false;
                if (this._listener != null)
                {
                    this._listener.Stop();
                }
                if (this._listenThread != null && !Thread.CurrentThread.Equals(_listenThread))
                {
                    this._listenThread.Abort();
                }
            }
        }

        #endregion

        #region Connection Processing

        private void ProcessIncomingConnections()
        {
            try
            {
                while (true)
                {
                    ProcessConnection();
                }
            }
            catch (Exception e)
            {
                Logger.Error("Exception while listening for connections, stopping listening : " + e.Message);
            }
            StopListening();
        }

        private void ProcessIncomingConnection()
        {
            try
            {
                ProcessConnection();
            }
            catch (Exception e)
            {
                Logger.Error("Exception while listening for connections, stopping listening : " + e.Message);
            }
            StopListening();
        }

        private void ProcessConnection()
        {
            TcpClient client = this._listener.AcceptTcpClient();
            Logger.Debug("Client connected from  " + client.Client.LocalEndPoint);
            try
            {
                ITcpOverUdptSocket socket = _transportManager.TCPTransport.OpenConnection(Name);
                ProxyTcpConnection proxyTcpConnection = new ProxyTcpConnection(client, socket);
                proxyTcpConnection.CloseConnection += delegate { ProxyConnectionOnClosed(proxyTcpConnection); };
                _openConnections.Add(proxyTcpConnection); 
                OnConnectionAccepted();
            }
            catch (Exception e)
            {
                Logger.Error("Failed to connect to named endpoint " + Name + " : " + e.Message);
                client.Close();
            }
        }

        private void ProxyConnectionOnClosed(ProxyTcpConnection connection)
        {
            if (_openConnections.Contains(connection))
            {
                _openConnections.Remove(connection);
            }
            OnConnectionClosed();
        }

        #endregion


        public void Close()
        {
            if (!Closing && !Closed)
            {
                Closing = true;
                StopListening();
                Closing = false;
                Closed = true;
            }
        }

        public void CloseConnections()
        {
            Logger.Debug("Closing all active connections for named connection " + Name);
            while (_openConnections != null && _openConnections.Count > 0)
            {
                var proxyConnection = _openConnections[0];
                _openConnections.RemoveAt(0);
                proxyConnection.Close();
            }
        }

        protected bool Closed { get; private set; }

        public bool Closing { get; private set; }
    }
}
