using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
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
        //private static readonly List<string> DefaultIPs = new List<String>(new String[] { "s1.i.dev.blits.me", "s2.i.dev.blits.me", "s3.i.dev.blits.me" });
        private static readonly List<string> DefaultIPs = new List<String>(new String[] { "i.dev.blits.me" });
#else
        private static readonly List<string> DefaultIPs = new List<String>(new String[] { "i.blits.me" });
#endif
        private static readonly List<int> DefaultPorts = new List<int>(new int[] { 443 });
        private ConnectionMaintainer _connectionMaintainer;
        private Thread _connectionMaintainerThread;
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
                return _connectionMaintainer.WebSocketClient;
            }
        }
        public WebSocketServer WebSocketServer
        {
            get
            {
                return _connectionMaintainer.WebSocketServer;
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
        }

        public void StartConnection(string version, X509Certificate2 cert)
        {
#if DEBUG
            Logger.Debug("Setting up communication with the cloud servers");
#endif
            _connectionMaintainer = new ConnectionMaintainer(version, Servers, Ports, cert);
            _connectionMaintainer.Disconnected += (sender, args) => OnDisconnect(args);
            _connectionMaintainer.Connected += (sender, args) => OnConnect(args);
            _connectionMaintainerThread = new Thread(_connectionMaintainer.Run) { IsBackground = true, Name = "_connectionMaintainerThread" };
#if DEBUG
            Logger.Debug("Starting connection manager");
#endif
            _connectionMaintainerThread.Start();
        }

        public bool isEstablished()
        {
            return _connectionMaintainer.IsConnectionEstablished();
        }

        public TRs Request<TRq, TRs>(TRq req)
            where TRq : Request
            where TRs : Response
        {
            if (!isEstablished())
            {
                throw new ConnectionException("Cannot send request, connection not established");
            }
            return _sendRequest<TRq, TRs>(req);
        }

        public void RequestAsync<TRq, TRs>(TRq req, Action<TRq, TRs, Exception> responseHandler)
            where TRq : Request
            where TRs : Response
        {
            if (!isEstablished())
            {
                throw new ConnectionException("Cannot send request, connection not established");
            }
            Thread asyncThread = new Thread(() =>
                {
                    try
                    {
                        TRs res = _sendRequest<TRq, TRs>(req);
                        try
                        {
                            responseHandler(req, res, null);
                        }
                        catch (Exception e)
                        {
                            Logger.Error("Assigned response handler threw an exception : " + e.Message, e);
                        }
                    }
                    catch (Exception e)
                    {
                        if (e is MessageException<TRs> && "WILL_NOT_PROCESS_INVALID_SESSION".Equals(((MessageException<TRs>)e).ErrorCode))
                        {
                            // something broke server side, cut the connection
                            _connectionMaintainer.ManualBreak("Invalid session, will reconnect.");
                        }
                        else
                        {
                            Logger.Error("Request failed", e);
                            responseHandler(req, null, e);
                        }
                    }
                });
            asyncThread.IsBackground = true;
            asyncThread.Start();
        }

        private TRs _sendRequest<TRq, TRs>(TRq req)
            where TRq : Request
            where TRs : Response
        {
            TRs response = WebSocketClient.SendRequest<TRq, TRs>(req);
            return response;
        }

        public void Close()
        {
            Logger.Debug("Shutting down connection maintainer");
            _connectionMaintainer.Disconnect();
            // now wait for 10 seconds for it to close, then kill it
            if(!_connectionMaintainerThread.Join(10000))
            {
                Logger.Warn("Connection maintainer didn't die in 10s, killing it");
                if(_connectionMaintainerThread.IsAlive)
                {
                    _connectionMaintainerThread.Abort();
                }
            }
        }


    }
}
