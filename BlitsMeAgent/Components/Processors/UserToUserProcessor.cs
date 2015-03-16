using System;
using System.Reflection;
using Gwupe.Cloud.Messaging.API;
using Gwupe.Cloud.Messaging.Response;
using log4net;

namespace Gwupe.Agent.Components.Processors
{
    internal abstract class UserToUserProcessor : Processor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(PresenceChangeProcessor));

        protected readonly GwupeClientAppContext _appContext;

        internal UserToUserProcessor(GwupeClientAppContext appContext)
        {
            _appContext = appContext;
        }

        public Response process(Request req)
        {
            var request = (UserToUserRequest)req;
            Engagement engagement = _appContext.EngagementManager.GetNewEngagement(request.username, request.shortCode);
            String requestTypeName = request.GetType().ToString();
            Type responseType = Type.GetType((requestTypeName.Substring(0, requestTypeName.Length - 2) + "Rs").Replace(".Request.", ".Response.") + ", Gwupe.Cloud");
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
                    Logger.Error("Failed to instantiate a response object for " + request.GetType() + " : " + e.Message, e);
                    return new ErrorRs() { error = "INTERNAL_SERVER_ERROR", errorMessage = "Failed to determine response type" };
                }
            }
            else
            {
                // Set the interaction and shortCode
                engagement.SecondParty.ActiveShortCode = request.shortCode;
                if (engagement.Interactions.CurrentInteraction == null)
                {
                    engagement.Interactions.StartInteraction(request.interactionId);

                }
                else if (request.interactionId != null)
                {
                    engagement.Interactions.CurrentOrNewInteraction.Id = request.interactionId;
                }
                response = ProcessWithEngagement(engagement, request);
                response.shortCode = _appContext.CurrentUserManager.ActiveShortCode;
                response.username = _appContext.CurrentUserManager.CurrentUser.Username;
                response.interactionId = engagement.Interactions.CurrentInteraction.Id;
            }
            return response;
        }

        internal abstract UserToUserResponse ProcessWithEngagement(Engagement engagement, UserToUserRequest request);
    }
}