using System;
using BlitsMe.Agent.Components.Functions;
using BlitsMe.Agent.Components.Notification;
using BlitsMe.Cloud.Messaging.API;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using log4net;

namespace BlitsMe.Agent.Components.Processors
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
                ((FileSend)engagement.getFunction("FileSend")).ProcessIncomingFileSendRequest(request.filename, request.fileSendId);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to process incoming File Send request : " + e.Message);
                response.error = "UNKNOWN_ERROR";
                response.errorMessage = "Failed to process incoming File Send request";
            }
            return response;
        }
    }
}