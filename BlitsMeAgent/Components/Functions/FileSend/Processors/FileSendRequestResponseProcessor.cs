using System;
using BlitsMe.Agent.Components.Processors;
using BlitsMe.Cloud.Messaging.API;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using log4net;

namespace BlitsMe.Agent.Components.Functions.FileSend.Processors
{
    internal class FileSendRequestResponseProcessor : UserToUserProcessor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (FileSendRequestResponseProcessor));

        public FileSendRequestResponseProcessor(BlitsMeClientAppContext appContext) : base(appContext)
        {
        }

        internal override UserToUserResponse ProcessWithEngagement(Engagement engagement, UserToUserRequest req)
        {
            FileSendRequestResponseRq request = (FileSendRequestResponseRq)req;
            FileSendRequestResponseRs response = new FileSendRequestResponseRs();
            try
            {
                ((Function)engagement.GetFunction("FileSend")).ProcessFileSendRequestResponse(request.accepted,request.fileSendId);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to process incoming FileSend request response : " + e.Message, e);
                response.error = "INTERNAL_SERVER_ERROR";
                response.errorMessage = "Failed to process incoming FileSend request response";
            }
            return response;
        }
    }
}