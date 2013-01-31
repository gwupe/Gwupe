using System;
using System.Collections.Generic;
using System.Threading;
using BlitsMe.Cloud.Messaging;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.API;
using Bauglir.Ex;
using System.IO;
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

        public bool isLoggedIn { get; set; }

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

        public ConnectionMaintainer(List<String> destinations, List<Int32> ports)
        {
            this.protocol = "message";
            servers = new List<Uri>();
            foreach (Int32 port in ports)
            {
                foreach (String destination in destinations)
                {
                    String uriString = "ws://" + destination + ":" + port + "/blitsme/ws";
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
                Response pong = webSocketClient.SendRequest(new PingRq());
                if (pong is Messaging.Response.PingRs)
                {
#if DEBUG
                    logger.Debug("Ping succeeded, round trip " + ((DateTime.Now.Ticks - startTime) / 10000) + " ms");
#endif
                    return true;
                }
                else
                {
                    logger.Error("Expected a pong from my ping, got a " + pong.ToString());
                }
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
            connection = new WebSocketClientConnection();
            connection.ConnectionClose += wsMessageHandler.onClose;
            connection.ConnectionOpen += wsMessageHandler.onOpen;
            connection.ConnectionRead += wsMessageHandler.onMessage;
            if (!connection.Start(uri.Host, uri.Port.ToString(), uri.PathAndQuery, false, "", protocol))
            {
                throw new IOException("Unknown error connecting to " + uri.ToString());
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
}
