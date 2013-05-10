using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using BlitsMe.Cloud.Messaging;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.API;
using Bauglir.Ex;
using System.IO;
using BlitsMe.Cloud.Messaging.Response;
using log4net;

namespace BlitsMe.Cloud.Communication
{
    public delegate void ConnectionEvent(object sender, EventArgs e);

    public class ConnectionMaintainer
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(ConnectionMaintainer));
        private bool maintainConnection;
        private bool connectionEstablished;
        private WebSocketMessageHandler wsMessageHandler;
        private WebSocketClientConnection connection;
        private String protocol { get; set; }
        private List<Uri> servers;
        public event ConnectionEvent Disconnect;
        public event ConnectionEvent Connect;
        private X509Certificate2 cacert;

        public AutoResetEvent connectionOpenEvent = new AutoResetEvent(false);
        public AutoResetEvent connectionCloseEvent = new AutoResetEvent(false);
        public AutoResetEvent wakeupManager = new AutoResetEvent(false);

        public WebSocketClient webSocketClient
        {
            get
            {
                return wsMessageHandler.webSocketClient;
            }
        }

        public Messaging.WebSocketServer webSocketServer
        {
            get
            {
                return wsMessageHandler.webSocketServer;
            }
        }

        public ConnectionMaintainer(String version, List<String> destinations, List<Int32> ports, X509Certificate2 cert)
        {
            this.protocol = "message";
            cacert = cert;
            servers = new List<Uri>();
            foreach (Int32 port in ports)
            {
                foreach (String destination in destinations)
                {
                    String uriString = "ws://" + destination + ":" + port + "/blitsme-ws/" + version;
                    try
                    {
                        Uri uri = new Uri(uriString);
                        servers.Add(uri);
                    }
                    catch (UriFormatException e)
                    {
                        logger.Error("Failed to parse URI " + uriString + ", skipping : " + e.Message);
                    }
                }
            }
            wsMessageHandler = new WebSocketMessageHandler(this);
        }

        public bool isConnectionEstablished()
        {
            return connectionEstablished;
        }

        public void disconnect()
        {
            maintainConnection = false;
            wakeupManager.Set();
        }

        public void run()
        {
            maintainConnection = true;
            connectionEstablished = false;
            while (maintainConnection)
            {
                while (!connectionEstablished && maintainConnection)
                {
                    foreach (Uri server in servers)
                    {
                        if (!maintainConnection)
                        {
                            break;
                        }
#if DEBUG
                        logger.Debug("Attempting to connect to server [" + server.ToString() + "]");
#endif

                        try
                        {
                            connect(server);
                            logger.Info("Successfully connected to server [" + server.ToString() + "]");
                            break;
                        }
                        catch (Exception e)
                        {
                            logger.Error("Failed to connect to server [" + server.ToString() + "] : " + e.Message);
                            continue;
                        }
                    }
                    // If we haven't established a connection
                    if (maintainConnection && !connectionEstablished)
                    {
#if DEBUG
                        logger.Debug("Failed to obtain a connection to a server, waiting for retry");
#endif
                        wakeupManager.Reset();
                        wakeupManager.WaitOne(10000);
                    }
                } // end get connection loop
                if (maintainConnection)
                {
                    // Will only come here if a connection was established
                    // Now we wait for 30s then check if the connection is still up
                    wakeupManager.Reset();
                    wakeupManager.WaitOne(30000);
                    if (maintainConnection && (!this.isConnected() || !ping()))
                    {
                        logger.Info("Connection seems to be down, marking it as such");
                        this.closeConnection(2, "Connection seems to be down");
                    }
                }
            } // end maintain connection loop
            this.closeConnection(1, "Disconnect requested");
        }


        public bool ping()
        {
            try
            {
                long startTime = DateTime.Now.Ticks;
                PingRs pong = webSocketClient.SendRequest<PingRq, PingRs>(new PingRq());
#if DEBUG
                logger.Debug("Ping succeeded, round trip " + ((DateTime.Now.Ticks - startTime) / 10000) + " ms");
#endif
                return true;
            }
            catch (Exception e)
            {
                logger.Error("Error while pinging : " + e.Message);
            }
            return false;
        }

        protected virtual void OnConnect(EventArgs e)
        {
            Connect(this, e);
        }

        protected virtual void OnDisconnect(EventArgs e)
        {
            Disconnect(this, e);
        }

        public void connect(Uri uri)
        {
            connection = new WebSocketClientSSLConnection(cacert);
            connection.ConnectionClose += wsMessageHandler.onClose;
            connection.ConnectionOpen += wsMessageHandler.onOpen;
            connection.ConnectionRead += wsMessageHandler.onMessage;
            try
            {
                if (!connection.Start(uri.Host, uri.Port.ToString(), uri.PathAndQuery, true, "", protocol))
                {
                    throw new IOException("Unknown error connecting to " + uri.ToString());
                }
            } catch(Exception e)
            {
                logger.Error("Failed to connect to server [" + uri +"] : " + e.Message);
                throw new IOException("Failed to connect to server [" + uri + "] : " + e.Message, e);
            }
            connectionEstablished = true;
            OnConnect(new EventArgs());
        }

        private bool isConnected()
        {
            return !(connection == null || connection.Closed || connection.Closing);
        }


        private void closeConnection(int code, String reason)
        {
            connectionEstablished = false;
            if (isConnected())
            {
                connection.Close(code, reason);
            }
            OnDisconnect(new EventArgs());
        }
    }

    internal class WebSocketClientSSLConnection : WebSocketClientConnection
    {
        private readonly X509Certificate2 _cacert;

        public WebSocketClientSSLConnection(X509Certificate2 cacert) : base()
        {
            _cacert = cacert;
        }


        protected override bool validateServerCertificate(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
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
            return isValid;
        }
    }
}
