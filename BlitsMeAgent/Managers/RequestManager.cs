using System;
using System.Net;
using BlitsMe.Agent.Components;
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
            _appContext.ConnectionManager.Connection.webSocketServer.RegisterProcessHandler("InitUDPConnection",(ProcessRequest<InitUDPConnectionRq,InitUDPConnectionRs>)ProcessInitUDPConnection);
            _appContext.ConnectionManager.Connection.webSocketServer.RegisterProcessHandler("ListenHandshake",(ProcessRequest<ListenHandshakeRq,ListenHandshakeRs>)ProcessListenHandshake);
            _appContext.ConnectionManager.Connection.webSocketServer.RegisterProcessHandler("PresenceChange",(ProcessRequest<PresenceChangeRq,PresenceChangeRs>)ProcessPresenceChange);
            _appContext.ConnectionManager.Connection.webSocketServer.RegisterProcessHandler("ChatMessage",(ProcessRequest<ChatMessageRq,ChatMessageRs>)ProcessChatMessage);
            _appContext.ConnectionManager.Connection.webSocketServer.RegisterProcessHandler("RDPRequest", (ProcessRequest<RDPRequestRq, RDPRequestRs>)ProcessRDPIncomingRequest);
            _appContext.ConnectionManager.Connection.webSocketServer.RegisterProcessHandler("RDPRequestResponse", (ProcessRequest<RDPRequestResponseRq,RDPRequestResponseRs>)ProcessRDPRequestResponse);
        }

        private InitUDPConnectionRs ProcessInitUDPConnection(InitUDPConnectionRq request)
        {
            InitUDPConnectionRs response = new InitUDPConnectionRs();
            try
            {
                PeerInfo self = _appContext.P2PManager.SetupTunnel(request.uniqueId, new IPEndPoint(IPAddress.Parse(request.facilitatorIP),Convert.ToInt32(request.facilitatorPort)));
                response.setUDPPeerInfo(self);
            }
            catch (Exception e)
            {
                Logger.Info("Failed to contact facilitator : " + e.Message);
                response.error = FACILITATOR_ERROR;
                response.errorMessage = "Failed to contact facilitator";
            }

            return response;
        }

        private ListenHandshakeRs ProcessListenHandshake(ListenHandshakeRq request)
        {
            ListenHandshakeRs response = new ListenHandshakeRs();
            try
            {
                var engagement = _appContext.EngagementManager.GetNewEngagement(request.username);
                var peerInfo = new PeerInfo(
                    new IPEndPoint(IPAddress.Parse(request.internalEndpointIp),Convert.ToInt32(request.internalEndpointPort)),
                    new IPEndPoint(IPAddress.Parse(request.externalEndpointIp),Convert.ToInt32(request.externalEndpointPort))
                    );
                engagement.SetupIncomingTunnel(_appContext.P2PManager.CompleteTunnel(request.uniqueId),peerInfo);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to start listening for UDP traffic from peer : " + e.Message);
                response.error = LISTEN_ERROR;
                response.errorMessage = "Failed to start listening for UDP traffic";
            }
            return response;
        }

        private PresenceChangeRs ProcessPresenceChange(PresenceChangeRq request)
        {
            var response = new PresenceChangeRs();
            try
            {
                _appContext.RosterManager.PresenceChange(request.user, request.presence, request.shortCode);
            }
            catch (Exception e)
            {
                    Logger.Error("Failed to process presence change : " + e.Message);
                    response.error = "UNKNOWN_ERROR";
                    response.errorMessage = "Failed to process presence change";
            }
            return response;
        }

        private ChatMessageRs ProcessChatMessage(ChatMessageRq request)
        {
            ChatMessageRs response = new ChatMessageRs();
            Engagement engagement = _appContext.EngagementManager.GetNewEngagement(request.from);
            if (engagement == null)
            {
                response.error = "INVALID_USERNAME";
                response.errorMessage = "Username was invalid";
            }
            else
            {
                try {
                    engagement.Chat.ReceiveChatMessage(request.message, request.chatId, request.fromShortCode);
                } catch(Exception e)
                {
                    Logger.Error("Failed to process chat message : " + e.Message);
                    response.error = "UNKNOWN_ERROR";
                    response.errorMessage = "Failed to process Chat request";
                }            }
            return response;
        }

        private RDPRequestRs ProcessRDPIncomingRequest(RDPRequestRq request)
        {
            RDPRequestRs response = new RDPRequestRs();
            Engagement engagement = _appContext.EngagementManager.GetNewEngagement(request.username);
            if (engagement == null)
            {
                response.error = "INVALID_USERNAME";
                response.errorMessage = "Username was invalid";
            }
            else
            {
                try
                {
                    engagement.ProcessIncomingRDPRequest(request.shortCode);
                } catch(Exception e)
                {
                    Logger.Error("Failed to process RDP incoming request : " + e.Message);
                    response.error = "UNKNOWN_ERROR";
                    response.errorMessage = "Failed to process RDP incoming request";
                }
            }
            return response;
        }

        private RDPRequestResponseRs ProcessRDPRequestResponse(RDPRequestResponseRq request)
        {
            RDPRequestResponseRs response = new RDPRequestResponseRs();
            Engagement engagement = _appContext.EngagementManager.GetNewEngagement(request.username);
            if (engagement == null)
            {
                response.error = "INVALID_USERNAME";
                response.errorMessage = "Username was invalid";
            }
            else
            {
                try
                {
                    engagement.ProcessRDPRequestResponse(request.shortCode, request.accepted);
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to process RDP request response : " + e.Message);
                    response.error = "UNKNOWN_ERROR";
                    response.errorMessage = "Failed to process RDP request response";
                }
            }
            return response;
        }



    }
}
