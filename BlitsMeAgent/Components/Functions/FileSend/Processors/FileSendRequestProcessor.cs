using System;
using BlitsMe.Agent.Components.Processors;
using BlitsMe.Cloud.Messaging.API;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using log4net;

namespace BlitsMe.Agent.Components.Functions.FileSend.Processors
{
    internal class FileSendRequestProcessor : UserToUserProcessor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(FileSendRequestProcessor));

        internal FileSendRequestProcessor(BlitsMeClientAppContext appContext) : base(appContext)
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