using System;
using BlitsMe.Agent.Components.Functions.FileSend.Processors;
using BlitsMe.Agent.Components.Functions.RemoteDesktop.Processors;
using BlitsMe.Agent.Components.Processors;
using log4net;
using ChatMessageProcessor = BlitsMe.Agent.Components.Functions.Chat.Processors.ChatMessageProcessor;

namespace BlitsMe.Agent.Managers
{
    public class RequestManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(RequestManager));
        private readonly BlitsMeClientAppContext _appContext;
        private bool _setup;
        public const String FACILITATOR_ERROR = "FACILITATOR_ERROR";
        public const String LISTEN_ERROR = "LISTEN_ERROR";

        public RequestManager()
        {
            this._appContext = BlitsMeClientAppContext.CurrentAppContext;
            _appContext.ConnectionManager.Connect += delegate(object sender, EventArgs args)
            {
                if (!_setup)
                {
                    RegisterRequestProcessors();
                }
            };
        }

        private void RegisterRequestProcessors()
        {
            // Link into the hooks so we can receive requests

            _appContext.ConnectionManager.Connection.WebSocketServer.RegisterProcessor("PresenceChange",
                new PresenceChangeProcessor(_appContext));
            _appContext.ConnectionManager.Connection.WebSocketServer.RegisterProcessor("ChatMessage",
                new ChatMessageProcessor(_appContext));
            _appContext.ConnectionManager.Connection.WebSocketServer.RegisterProcessor("InitUDPConnection",
                new InitUDPConnectionProcessor(_appContext));
            _appContext.ConnectionManager.Connection.WebSocketServer.RegisterProcessor("ListenHandshake",
                new ListenHandshakeProcessor(_appContext));
            _appContext.ConnectionManager.Connection.WebSocketServer.RegisterProcessor("RDPRequest",
                new RDPRequestProcessor(_appContext));
            _appContext.ConnectionManager.Connection.WebSocketServer.RegisterProcessor("RDPRequestResponse",
                new RDPRequestResponseProcessor(_appContext));
            _appContext.ConnectionManager.Connection.WebSocketServer.RegisterProcessor("FileSendRequest",
                new FileSendRequestProcessor(_appContext));
            _appContext.ConnectionManager.Connection.WebSocketServer.RegisterProcessor("FileSendRequestResponse",
                new FileSendRequestResponseProcessor(_appContext));
            _appContext.ConnectionManager.Connection.WebSocketServer.RegisterProcessor("Subscribe",
                new SubscribeProcessor(_appContext));
            _appContext.ConnectionManager.Connection.WebSocketServer.RegisterProcessor("InitRepeatedConnection", 
                new InitRepeatedConnectionProcessor(_appContext));
            _appContext.ConnectionManager.Connection.WebSocketServer.RegisterProcessor("ServerNotification",
                new ServerNotificationProcessor(_appContext));
            _appContext.ConnectionManager.Connection.WebSocketServer.RegisterProcessor("NotifyChange",
                new NotifyChangeProcessor(_appContext));
            _setup = true;
        }
    }
}
