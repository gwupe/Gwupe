using System;
using Gwupe.Agent.Components.Processors;
using Gwupe.Cloud.Messaging.API;
using Gwupe.Cloud.Messaging.Request;
using Gwupe.Cloud.Messaging.Response;
using log4net;

namespace Gwupe.Agent.Components.Functions.RemoteDesktop.Processors
{
    internal class RDPRequestResponseProcessor : UserToUserProcessor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(RDPRequestResponseProcessor));

        internal RDPRequestResponseProcessor(GwupeClientAppContext appContext)
            : base(appContext)
        {
        }

        internal override UserToUserResponse ProcessWithEngagement(Engagement engagement, UserToUserRequest req)
        {
            RDPRequestResponseRq request = (RDPRequestResponseRq)req;
            RDPRequestResponseRs response = new RDPRequestResponseRs();
            try
            {
                ((Function)engagement.GetFunction("RemoteDesktop")).ProcessRemoteDesktopRequestResponse(request);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to process incoming RDP request response : " + e.Message, e);
                response.error = "INTERNAL_SERVER_ERROR";
                response.errorMessage = "Failed to process incoming RDP request response";
            }
            return response;
        }


    }
}