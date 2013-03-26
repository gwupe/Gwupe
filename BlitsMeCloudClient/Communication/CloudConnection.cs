using System;
using System.Collections.Generic;
using System.Threading;
using BlitsMe.Cloud.Messaging.API;
using BlitsMe.Cloud.Exceptions;
using BlitsMe.Cloud.Messaging;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using log4net;

namespace BlitsMe.Cloud.Communication
{
    public class CloudConnection
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(CloudConnection));
#if DEBUG
        private static readonly List<string> DefaultIPs = new List<String>(new String[] { "s1.i.dev.blits.me", "s2.i.dev.blits.me", "s3.i.dev.blits.me" });
#else
        private static readonly List<string> DefaultIPs = new List<String>(new String[] { "s1.i.blits.me", "s2.i.blits.me", "s3.i.blits.me" });
#endif
        private static readonly List<int> DefaultPorts = new List<int>(new int[] { 8443 });
        private readonly ConnectionMaintainer _connectionMaintainer;
        private readonly Thread _connectionMaintainerThread;
        public List<string> Servers;
        public List<int> Ports;
        public event ConnectionEvent Disconnect;

        public void OnDisconnect(EventArgs e)
        {
            ConnectionEvent handler = Disconnect;
            if (handler != null) handler(this, e);
        }

        public event ConnectionEvent Connect;

        public void OnConnect(EventArgs e)
        {
            ConnectionEvent handler = Connect;
            if (handler != null) handler(this, e);
        }

        private WebSocketClient WebSocketClient
        {
            get
            {
                return _connectionMaintainer.webSocketClient;
            }
        }
        public WebSocketServer WebSocketServer
        {
            get
            {
                return _connectionMaintainer.webSocketServer;
            }
        }

        public CloudConnection()
            : this(DefaultIPs, DefaultPorts)
        {
        }

        public CloudConnection(List<string> servers)
            : this(servers, DefaultPorts)
        {
        }


        public CloudConnection(List<string> connectServers, List<int> connectPorts)
        {
            Servers = (connectServers == null || connectServers.Count == 0) ? DefaultIPs : connectServers;
            Ports = (connectPorts == null || connectPorts.Count == 0) ? DefaultPorts : connectPorts;
#if DEBUG
            Logger.Debug("Setting up communication with the cloud servers");
#endif
            _connectionMaintainer = new ConnectionMaintainer(Servers, Ports);
            _connectionMaintainer.Disconnect += (sender, args) => OnDisconnect(args);
            _connectionMaintainer.Connect += (sender, args) => OnConnect(args);
            _connectionMaintainerThread = new Thread(_connectionMaintainer.run) { IsBackground = true, Name = "_connectionMaintainerThread"};
#if DEBUG
            Logger.Debug("Starting connection manager");
#endif
            _connectionMaintainerThread.Start();
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
            return _sendRequest(req);
        }

        public void RequestAsync(Request req, Action<Request,Response> responseHandler)
        {
            if (!isEstablished())
            {
                throw new ConnectionException("Cannot send request, connection not established");
            }
            Thread asyncThread = new Thread(() =>
                {
                    try
                    {
                        Response res = _sendRequest(req);
                        try
                        {
                            responseHandler(req, res);
                        } catch (Exception e)
                        {
                          Logger.Error("Assigned response handler threw an exception : " + e.Message,e);
                        }
                    } catch(Exception e)
                    {
                        Logger.Error("Failed to run response handler for request",e);
                        responseHandler(req,new ErrorRs() { error = "REQUEST_ERROR", errorMessage = e.Message });
                    }
                });
            asyncThread.IsBackground = true;
            asyncThread.Start();
        }

        private Response _sendRequest(Request req)
        {
            Response response = WebSocketClient.SendRequest(req);
            return response;
        }

        public void Close()
        {
            _connectionMaintainer.disconnect();
            _connectionMaintainerThread.Join();
        }


    }
}
