using System;
using BlitsMe.Cloud.Messaging.API;
using BlitsMe.Cloud.Messaging.Response;
using log4net;

namespace BlitsMe.Agent.Components.Processors
{
    internal abstract class UserToUserProcessor : Processor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (PresenceChangeProcessor));

        protected readonly BlitsMeClientAppContext _appContext;

        internal UserToUserProcessor(BlitsMeClientAppContext appContext)
        {
            _appContext = appContext;
        }

        public Response process(Request req)
        {
            var request = (UserToUserRequest) req;
            Engagement engagement = _appContext.EngagementManager.GetNewEngagement(request.username);
            String requestTypeName = request.GetType().ToString();
            Type responseType = Type.GetType(requestTypeName.Substring(0, requestTypeName.Length - 2) + "Rs");
            UserToUserResponse response = null;
            if (engagement == null)
            {
                try
                {
                    response = (UserToUserResponse)responseType.GetConstructor(Type.EmptyTypes).Invoke(new object[] { });
                    response.error = "INVALID_USERNAME";
                    response.errorMessage = "Username was invalid";
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to instantiate a response object for " + request.GetType() + " : " + e.Message,e);
                    return new ErrorRs() { error = "INTERNAL_SERVER_ERROR", errorMessage = "Failed to determine response type" };
                }
            }
            else
            {
                try
                {
                    engagement.SecondParty.ShortCode = request.shortCode;
                    response = ProcessWithEngagement(engagement, request);
                    response.shortCode = engagement.SecondParty.ShortCode;
                    response.username = engagement.SecondParty.Username;
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to process the user to user request : " + e.Message,e);
                    response = (UserToUserResponse)responseType.GetConstructor(Type.EmptyTypes).Invoke(new object[] { });
                    response.error = "INTERNAL_SERVER_ERROR";
                    response.errorMessage = "Failed to process user to user request";
                }
            }
            return response;
        }
             
        internal abstract UserToUserResponse ProcessWithEngagement(Engagement engagement, UserToUserRequest request);
    }
}