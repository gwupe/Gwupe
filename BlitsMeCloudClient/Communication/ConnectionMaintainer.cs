using System;
using System.Collections.Generic;
using System.Net.Security;
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
#if DEBUG
                Logger.Debug("Ping to blitsme [" + _connection.Client.Client.RemoteEndPoint + "] succeeded, round trip " + LastPing + " ms");
#endif
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
            _connection.ConnectionClose += _wsMessageHandler.onClose;
            _connection.ConnectionClose += delegate { OnDisconnect(EventArgs.Empty); };
            _connection.ConnectionOpen += _wsMessageHandler.onOpen;
            _connection.ConnectionOpen += delegate { OnConnect(EventArgs.Empty); };
            // we no longer do it this way, but process it via a called function on full
            //_connection.ConnectionRead += _wsMessageHandler.onMessage;
            try
            {
                if (!_connection.Start(uri.Host, uri.Port.ToString(), uri.PathAndQuery, true, "", Protocol))
                {
                    throw new IOException("Unknown error connecting to " + uri.ToString());
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to connect to server [" + uri + "] : " + e.Message);
                throw new IOException("Failed to connect to server [" + uri + "] : " + e.Message, e);
            }
            //OnConnect(new EventArgs());
        }

        private bool IsConnected()
        {
            return !(_connection == null || _connection.Closed || _connection.Closing);
        }


        private void CloseConnection(int code, String reason)
        {
            if (IsConnected())
            {
                _connection.Close(code, reason);
            }
            OnDisconnect(new EventArgs());
        }
    }

    internal class WebSocketClientSSLConnection : WebSocketClientConnection
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(WebSocketClientSSLConnection));
        private readonly X509Certificate2 _cacert;
        private readonly WebSocketMessageHandler _wsMessageHandler;

        public WebSocketClientSSLConnection(X509Certificate2 cacert, WebSocketMessageHandler wsMessageHandler)
            : base()
        {
            _cacert = cacert;
            _wsMessageHandler = wsMessageHandler;
            this.FullDataProcess = true;
        }

        protected override void ProcessTextFull(string message)
        {
            _wsMessageHandler.ProcessMessage(message);
        }

        protected override bool validateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            bool isValid = false;
            if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors)
            {
                X509Chain chain0 = new X509Chain();
                chain0.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                // add all your extra certificate chain
                chain0.ChainPolicy.ExtraStore.Add(new X509Certificate2(_cacert));
                chain0.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
                isValid = chain0.Build((X509Certificate2)certificate);
            }
            Logger.Debug("Checking cert valid, " + isValid);
            return isValid;
        }
    }
}
