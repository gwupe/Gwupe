using System;
using BlitsMe.Agent.Components.Processors;
using BlitsMe.Cloud.Messaging.API;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using log4net;

namespace BlitsMe.Agent.Components.Functions.RemoteDesktop.Processors
{
    internal class RDPRequestProcessor : UserToUserProcessor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(RDPRequestProcessor));
        private readonly BlitsMeClientAppContext _appContext;

        internal RDPRequestProcessor(BlitsMeClientAppContext appContext) : base(appContext)
        {
            _appContext = appContext;
        }

        internal override UserToUserResponse ProcessWithEngagement(Engagement engagement, UserToUserRequest req)
        {
            RDPRequestRq request = (RDPRequestRq) req;
            RDPRequestRs response = new RDPRequestRs();
            try
            {
                ((Function)engagement.getFunction("RemoteDesktop")).ProcessIncomingRDPRequest(request.shortCode);
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