using System;
using Gwupe.Agent.Components.Processors;
using Gwupe.Cloud.Messaging.API;
using Gwupe.Cloud.Messaging.Request;
using Gwupe.Cloud.Messaging.Response;
using log4net;

namespace Gwupe.Agent.Components.Functions.FileSend.Processors
{
    internal class FileSendRequestResponseProcessor : UserToUserProcessor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (FileSendRequestResponseProcessor));

        public FileSendRequestResponseProcessor(GwupeClientAppContext appContext) : base(appContext)
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