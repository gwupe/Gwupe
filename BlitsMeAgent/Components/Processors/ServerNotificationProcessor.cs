using System;
using Gwupe.Agent.Components.Person;
using Gwupe.Cloud.Messaging.API;
using Gwupe.Cloud.Messaging.Request;
using Gwupe.Cloud.Messaging.Response;
using log4net;
using log4net.Repository.Hierarchy;

namespace Gwupe.Agent.Components.Processors
{
    public class ServerNotificationProcessor : Processor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (ServerNotificationProcessor));

        private readonly GwupeClientAppContext _appContext;

        public ServerNotificationProcessor(GwupeClientAppContext appContext)
        {
            _appContext = appContext;
        }

        public Response process(Request req)
        {
            var request = (ServerNotificationRq)req;
            var response = new ServerNotificationRs();
            try
            {
                if (ServerNotificationCode.INVALID_SESSION.ToString().Equals(request.code))
                {
                    GwupeClientAppContext.CurrentAppContext.LoginManager.InvalidateSession();
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to process server notification : " + e.Message,e);
                response.error = "UNKNOWN_ERROR";
                response.errorMessage = "Failed to process server notification";
            }
            return response;


        }
    }
}