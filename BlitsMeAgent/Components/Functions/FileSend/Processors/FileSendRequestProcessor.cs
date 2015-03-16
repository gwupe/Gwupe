using System;
using Gwupe.Agent.Components.Processors;
using Gwupe.Cloud.Messaging.API;
using Gwupe.Cloud.Messaging.Request;
using Gwupe.Cloud.Messaging.Response;
using log4net;

namespace Gwupe.Agent.Components.Functions.FileSend.Processors
{
    internal class FileSendRequestProcessor : UserToUserProcessor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(FileSendRequestProcessor));

        internal FileSendRequestProcessor(GwupeClientAppContext appContext) : base(appContext)
        {
        }

        internal override UserToUserResponse ProcessWithEngagement(Engagement engagement, UserToUserRequest req)
        {
            FileSendRequestRq request = (FileSendRequestRq) req;
            FileSendRequestRs response = new FileSendRequestRs();
            try
            {
                ((Function)engagement.GetFunction("FileSend")).ProcessIncomingFileSendRequest(request.filename, request.fileSendId, request.fileSize);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to process incoming File Send request : " + e.Message,e);
                response.error = "UNKNOWN_ERROR";
                response.errorMessage = "Failed to process incoming File Send request";
            }
            return response;
        }
    }
}