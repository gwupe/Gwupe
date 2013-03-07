using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using BlitsMe.Agent.Components;
using BlitsMe.Agent.Components.Processors;
using BlitsMe.Cloud.Messaging;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using BlitsMe.Communication.P2P.RUDP.Utils;
using log4net;

namespace BlitsMe.Agent.Managers
{
    public class RequestManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(RequestManager));
        private readonly BlitsMeClientAppContext _appContext;
        public const String FACILITATOR_ERROR = "FACILITATOR_ERROR";
        public const String LISTEN_ERROR = "LISTEN_ERROR";

        public RequestManager(BlitsMeClientAppContext appContext)
        {
            this._appContext = appContext;
            // Link into the hooks so we can receive requests

            _appContext.ConnectionManager.Connection.webSocketServer.RegisterProcessor("PresenceChange",new PresenceChangeProcessor(_appContext));
            _appContext.ConnectionManager.Connection.webSocketServer.RegisterProcessor("ChatMessage",new ChatMessageProcessor(_appContext));
            _appContext.ConnectionManager.Connection.webSocketServer.RegisterProcessor("InitUDPConnection", new InitUDPConnectionProcessor(_appContext));
            _appContext.ConnectionManager.Connection.webSocketServer.RegisterProcessor("ListenHandshake", new ListenHandshakeProcessor(_appContext));
            _appContext.ConnectionManager.Connection.webSocketServer.RegisterProcessor("RDPRequest", new RDPRequestProcessor(_appContext));
            _appContext.ConnectionManager.Connection.webSocketServer.RegisterProcessor("RDPRequestResponse", new RDPRequestResponseProcessor(_appContext));
            _appContext.ConnectionManager.Connection.webSocketServer.RegisterProcessor("FileSendRequest", new FileSendRequestProcessor(_appContext));
            _appContext.ConnectionManager.Connection.webSocketServer.RegisterProcessor("FileSendRequestResponse", new FileSendRequestResponseProcessor(_appContext));
        }
    }
}
