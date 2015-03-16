using System;
using Gwupe.Agent.Components.Processors;
using Gwupe.Cloud.Messaging.API;
using Gwupe.Cloud.Messaging.Request;
using Gwupe.Cloud.Messaging.Response;
using log4net;

namespace Gwupe.Agent.Components.Functions.RemoteDesktop.Processors
{
    internal class RDPRequestProcessor : UserToUserProcessor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(RDPRequestProcessor));
        private readonly GwupeClientAppContext _appContext;

        internal RDPRequestProcessor(GwupeClientAppContext appContext) : base(appContext)
        {
            _appContext = appContext;
        }

        internal override UserToUserResponse ProcessWithEngagement(Engagement engagement, UserToUserRequest req)
        {
            RDPRequestRq request = (RDPRequestRq) req;
            RDPRequestRs response = new RDPRequestRs();
            try
            {
                ((Function)engagement.GetFunction("RemoteDesktop")).ProcessIncomingRemoteDesktopRequest(request.shortCode);
            } catch (Exception e)
            {
                Logger.Error("Failed to process incoming RDP request : " + e.Message,e);
                response.error = "INTERNAL_SERVER_ERROR";
                response.errorMessage = "Failed to process incoming RDP request";
            }
            return response;
        }
    }
}