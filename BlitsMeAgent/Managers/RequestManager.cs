using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using BlitsMe.Agent.Components;
using BlitsMe.Agent.Components.Processors;
using BlitsMe.Cloud.Messaging;
using BlitsMe.Cloud.Messaging.API;
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
            /*
            _appContext.ConnectionManager.Connection.webSocketServer.RegisterProcessHandler("RDPRequest", (ProcessRequest<RDPRequestRq, RDPRequestRs>)ProcessRDPIncomingRequest);
            _appContext.ConnectionManager.Connection.webSocketServer.RegisterProcessHandler("RDPRequestResponse", (ProcessRequest<RDPRequestResponseRq,RDPRequestResponseRs>)ProcessRDPRequestResponse);
            _appContext.ConnectionManager.Connection.webSocketServer.RegisterProcessHandler("FileSendRequest", (ProcessRequest<FileSendRequestRq, FileSendRequestRs>) ProcessFileSendRequest);
            _appContext.ConnectionManager.Connection.webSocketServer.RegisterProcessHandler("FileSendRequestResponse", (ProcessRequest<UserToUserRequest, UserToUserResponse>)GenericUserToUserRequestHandler);
             */
            _appContext.ConnectionManager.Connection.webSocketServer.RegisterProcessor("PresenceChange",new PresenceChangeProcessor(_appContext));
            _appContext.ConnectionManager.Connection.webSocketServer.RegisterProcessor("ChatMessage",new ChatMessageProcessor(_appContext));
            _appContext.ConnectionManager.Connection.webSocketServer.RegisterProcessor("InitUDPConnection", new InitUDPConnectionProcessor(_appContext));
            _appContext.ConnectionManager.Connection.webSocketServer.RegisterProcessor("ListenHandshake", new ListenHandshakeProcessor(_appContext));
            _appContext.ConnectionManager.Connection.webSocketServer.RegisterProcessor("RDPRequest", new RDPRequestProcessor(_appContext));
            _appContext.ConnectionManager.Connection.webSocketServer.RegisterProcessor("RDPRequestResponse", new RDPRequestResponseProcessor(_appContext));
        }

        private UserToUserResponse GenericUserToUserRequestHandler(UserToUserRequest request)
        {
            String requestType = request.GetType().Name;
            String processorName = requestType.Substring(0, requestType.Length - 2);
            Type responseType = Type.GetType("BlitsMe.Cloud.Messaging.Response." + processorName + "Rs");
            UserToUserResponse response = null;
            try
            {
                response =
                    (UserToUserResponse) responseType.GetConstructor(Type.EmptyTypes).Invoke(new object[] {});
                Engagement engagement = _appContext.EngagementManager.GetNewEngagement(request.username);
                if (engagement == null)
                {
                    response.error = "INVALID_USERNAME";
                    response.errorMessage = "Username was invalid";
                }
                else
                {
                    response.shortCode = engagement.SecondParty.ShortCode;
                    response.username = engagement.SecondParty.Username;
                    try
                    {
                        var theMethods = from mi in engagement.GetType().GetMethods()
                                         let p = mi.GetParameters()
                                         where p.Length == 2
                                             && p[0].ParameterType == request.GetType()
                                             && p[1].ParameterType == responseType
                                             && mi.ReturnType == typeof(void)
                                         select mi;
                        foreach (MethodInfo methodInfo in theMethods)
                        {
                            Logger.Debug("Method matches " + methodInfo.Name);
                        }
                        //engagement.ProcessIncomingFileSendRequest(request.filename, request.fileSendId);
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Failed to process incoming File Send request : " + e.Message);
                        response.error = "UNKNOWN_ERROR";
                        response.errorMessage = "Failed to process incoming File Send request";
                    }
                }
            } catch (Exception e)
            {
                Logger.Error("Failed to process request with generic handler : " + e.Message,e);
            }
            return response;
        }

        private FileSendRequestRs ProcessFileSendRequest(FileSendRequestRq request)
        {
            FileSendRequestRs response = new FileSendRequestRs();
            Engagement engagement = _appContext.EngagementManager.GetNewEngagement(request.username);
            if (engagement == null)
            {
                response.error = "INVALID_USERNAME";
                response.errorMessage = "Username was invalid";
            }
            else
            {
                response.shortCode = engagement.SecondParty.ShortCode;
                response.username = engagement.SecondParty.Username;
                try
                {
                    engagement.ProcessIncomingFileSendRequest(request.filename, request.fileSendId);
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to process incoming File Send request : " + e.Message);
                    response.error = "UNKNOWN_ERROR";
                    response.errorMessage = "Failed to process incoming File Send request";
                }
            }
            return response;
        }

    }
}
