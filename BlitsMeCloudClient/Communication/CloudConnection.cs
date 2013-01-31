using System;
using System.Collections.Generic;
using System.Threading;
using BlitsMe.Cloud.Messaging.API;
using BlitsMe.Cloud.Exceptions;
using BlitsMe.Cloud.Messaging;
using BlitsMe.Cloud.Messaging.Request;
using log4net;

namespace BlitsMe.Cloud.Communication
{
    public class CloudConnection
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(CloudConnection));
#if DEBUG
        private static List<string> defaultIPs = new List<String>(new String[] { "s1.i.dev.blits.me", "s2.i.dev.blits.me", "s3.i.dev.blits.me" });
#else
        private static List<string> defaultIPs = new List<String>(new String[] { "s1.i.blits.me", "s2.i.blits.me", "s3.i.blits.me" });
#endif
        private static List<int> defaultPorts = new List<int>(new int[] { 10230, 443, 80 });
        private LoginRq loginRq;
        public Messaging.Response.LoginRs loginRs { get; private set; }
        private ConnectionMaintainer _connectionMaintainer;
        public bool isLoggedIn;
        private readonly Thread _connectionManagerThread;
        public List<string> servers;
        public List<int> ports;
        public event ConnectionEvent Disconnect;
        public event ConnectionEvent Connect;

        private WebSocketClient webSocketClient
        {
            get
            {
                return _connectionMaintainer.webSocketClient;
            }
        }
        public WebSocketServer webSocketServer
        {
            get
            {
                return _connectionMaintainer.webSocketServer;
            }
        }

        public CloudConnection()
            : this(defaultIPs, defaultPorts)
        {
        }

        public CloudConnection(List<string> servers)
            : this(servers, defaultPorts)
        {
        }


        public CloudConnection(List<string> connectServers, List<int> connectPorts)
        {
            servers = (connectServers == null || connectServers.Count == 0) ? defaultIPs : connectServers;
            ports = (connectPorts == null || connectPorts.Count == 0) ? defaultPorts : connectPorts;
#if DEBUG
            logger.Debug("Setting up communication with the cloud servers");
#endif
            _connectionMaintainer = new ConnectionMaintainer(servers, ports);
            _connectionMaintainer.Disconnect += disconnected;
            _connectionMaintainer.Connect += connected;
            _connectionManagerThread = new Thread(_connectionMaintainer.run) { IsBackground = true, Name = "_connectionManagerThread"};
#if DEBUG
            logger.Debug("Starting connection manager");
#endif
            _connectionManagerThread.Start();
        }

        private void disconnected(object sender, EventArgs e) {
#if DEBUG
            logger.Debug("Connection was disconnected");
#endif
            this.isLoggedIn = false;
            Disconnect(this, e);
        }

        private void connected(object sender, EventArgs e)
        {
            Connect(this, e);
        }

        public bool isEstablished()
        {
            return _connectionMaintainer.isConnectionEstablished();
        }

        public Response Request(Request req)
        {
            if (!isEstablished())
            {
                throw new ConnectionException("Cannot send request, connection not established");
            }
            if (!isLoggedIn)
            {
                throw new ConnectionException("Cannot send request, not logged in");
            }
            return _sendRequest(req);
        }

        private Response _sendRequest(Request req)
        {
            Response response = webSocketClient.SendRequest(req);
            return response;
        }


        public void login(String username, String profile, String workstation, String passwordHash)
        {
            loginRq = new LoginRq();
            loginRq.passwordDigest = passwordHash;
            loginRq.username = username;
            loginRq.profile = profile;
            loginRq.workstation = workstation;
            login();
        }

        private void login()
        {
            if (!isEstablished())
            {
                throw new ConnectionException("Cannot send login request, connection not established");
            }
            var loginRs = (Messaging.Response.LoginRs)_sendRequest(loginRq);
            if (!loginRs.loggedIn)
            {
                throw new LoginException("Failed to login, server responded with : " + loginRs.errorMessage, loginRs.error);
            }
            this.loginRs = loginRs;
            isLoggedIn = true;
        }

        public void close()
        {
            _connectionMaintainer.disconnect();
            _connectionManagerThread.Join();
        }


    }
}
