using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using BlitsMe.Cloud.Messaging;
using BlitsMe.Cloud.Messaging.Request;
using Bauglir.Ex;
using System.IO;
using BlitsMe.Cloud.Messaging.Response;
using log4net;

namespace BlitsMe.Cloud.Communication
{
    public delegate void ConnectionEvent(object sender, EventArgs e);

    public class ConnectionMaintainer
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ConnectionMaintainer));
        private bool _maintainConnection;
        private bool _connectionEstablished;
        private readonly WebSocketMessageHandler _wsMessageHandler;
        private WebSocketClientConnection _connection;
        private String Protocol { get; set; }
        private readonly List<Uri> _servers;
        public event ConnectionEvent Disconnected;
        public event ConnectionEvent Connected;
        private readonly X509Certificate2 _cacert;
        public long LastPing { get; private set; }

        public AutoResetEvent ConnectionOpenEvent = new AutoResetEvent(false);
        public AutoResetEvent ConnectionCloseEvent = new AutoResetEvent(false);
        public AutoResetEvent WakeupManager = new AutoResetEvent(false);

        public WebSocketClient WebSocketClient
        {
            get
            {
                return _wsMessageHandler.WebSocketClient;
            }
        }

        public Messaging.WebSocketServer WebSocketServer
        {
            get
            {
                return _wsMessageHandler.WebSocketServer;
            }
        }

        public ConnectionMaintainer(String version, List<String> destinations, List<Int32> ports, X509Certificate2 cert)
        {
            this.Protocol = "message";
            _cacert = cert;
            _servers = new List<Uri>();
            foreach (Int32 port in ports)
            {
                foreach (String destination in destinations)
                {
                    String uriString = "ws://" + destination + ":" + port + "/blitsme-ws/ws";
                    try
                    {
                        Uri uri = new Uri(uriString);
                        _servers.Add(uri);
                    }
                    catch (UriFormatException e)
                    {
                        Logger.Error("Failed to parse URI " + uriString + ", skipping : " + e.Message);
                    }
                }
            }
            _wsMessageHandler = new WebSocketMessageHandler(this);
        }

        public bool IsConnectionEstablished()
        {
            return _connectionEstablished;
        }

        public void Disconnect()
        {
            _maintainConnection = false;
            WakeupManager.Set();
        }

        public void Run()
        {
            _maintainConnection = true;
            _connectionEstablished = false;
            while (_maintainConnection)
            {
                while (!_connectionEstablished && _maintainConnection)
                {
                    foreach (Uri server in _servers)
                    {
                        if (!_maintainConnection)
                        {
                            break;
                        }
#if DEBUG
                        Logger.Debug("Attempting to connect to server [" + server.ToString() + "]");
#endif

                        try
                        {
                            Connect(server);
                            Logger.Info("Successfully connected to server [" + server.ToString() + "]");
                            // wait here for a second to initialise
                            Thread.Sleep(5000);
                            break;
                        }
                        catch (Exception e)
                        {
                            Logger.Error("Failed to connect to server [" + server.ToString() + "] : " + e.Message);
                            continue;
                        }
                    }
                    // If we haven't established a connection
                    if (_maintainConnection && !_connectionEstablished)
                    {
#if DEBUG
                        Logger.Debug("Failed to obtain a connection to a server, waiting for retry");
#endif
                        WakeupManager.Reset();
                        WakeupManager.WaitOne(10000);
                    }
                } // end get connection loop
                if (_maintainConnection)
                {
                    // Will only come here if a connection was established
                    // Now we wait for 30s then check if the connection is still up
                    WakeupManager.Reset();
                    WakeupManager.WaitOne(30000);
                    if (_maintainConnection && (!this.IsConnected() || !Ping()))
                    {
                        Logger.Info("Connection seems to be down, marking it as such");
                        this.CloseConnection(WebSocketCloseCode.DataError, "Connection seems to be down");
                    }
                }
            } // end maintain connection loop
            this.CloseConnection(WebSocketCloseCode.Normal, "Disconnect requested");
        }


        public bool Ping()
        {
            try
            {
                long startTime = Environment.TickCount;
                WebSocketClient.SendRequest<PingRq, PingRs>(new PingRq());
                LastPing = Environment.TickCount - startTime;
                Logger.Debug("Ping to blitsme [" + _connection.Client.Client.RemoteEndPoint + "] succeeded, round trip " + LastPing + " ms");
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("Error while pinging : " + e.Message);
            }
            return false;
        }

        protected virtual void OnConnect(EventArgs e)
        {
            _connectionEstablished = true;
            Connected(this, e);
        }

        protected virtual void OnDisconnect(EventArgs e)
        {
            if (_connectionEstablished)
            {
                _connectionEstablished = false;
                Disconnected(this, e);
            }
        }

        public void Connect(Uri uri)
        {
            _connection = new WebSocketClientSSLConnection(_cacert, _wsMessageHandler);
            _connection.ConnectionClose += _wsMessageHandler.OnClose;
            _connection.ConnectionClose += delegate { OnDisconnect(EventArgs.Empty); };
            _connection.ConnectionOpen += _wsMessageHandler.OnOpen;
            _connection.ConnectionOpen += delegate { OnConnect(EventArgs.Empty); };
            // we no longer do it this way, but process it via a called function on full text being read
            //_connection.ConnectionRead += _wsMessageHandler.onMessage;

            // this whole starting of another thread to run the connection is because sometimes the connection hangs. We have set the read timeout
            // on the connection to 5 mins, but lets watch it here as well to make sure that the whole connection system doesn't hang.
            bool connected = false;
            Thread startThread = new Thread(() =>
            {
                try
                {
                    if (!_connection.Start(uri.Host, uri.Port.ToString(), uri.PathAndQuery, true, "", Protocol))
                    {
                        throw new IOException("Unknown error connecting to " + uri.ToString());
                    }
                    connected = true;
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to connect to server [" + uri + "] : " + e.Message);
                }
            }) { Name = "ConnectionStarter" };
            startThread.Start();
            if (!startThread.Join(360000))// 6 minute timeout
            {
                startThread.Abort();
                // connect didn't come back within the timeout period
                throw new Exception("Failed to start connection within timeout");
            }
            if (!connected)
            {
                throw new IOException("Failed to connect to server [" + uri + "]");
            }
            Logger.Debug("Connection Complete");
            //OnConnect(new EventArgs());
        }

        private bool IsConnected()
        {
            try
            {
                // this can sometimes throw a null pointer
                return !(_connection == null || _connection.Closed || _connection.Closing);
            }
            catch (Exception e)
            {
                return false;
            }
        }


        private void CloseConnection(int code, String reason)
        {
            if (IsConnected())
            {
                _connection.Close(code, reason);
            }
            OnDisconnect(new EventArgs());
        }

        public void ManualBreak(string reason)
        {
            CloseConnection(WebSocketCloseCode.DataError, reason);
        }
    }
}
