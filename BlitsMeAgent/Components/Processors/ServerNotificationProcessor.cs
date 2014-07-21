using System;
using BlitsMe.Agent.Components.Person;
using BlitsMe.Cloud.Messaging.API;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using log4net;
using log4net.Repository.Hierarchy;

namespace BlitsMe.Agent.Components.Processors
{
    public class ServerNotificationProcessor : Processor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (ServerNotificationProcessor));

        private readonly BlitsMeClientAppContext _appContext;

        public ServerNotificationProcessor(BlitsMeClientAppContext appContext)
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
                    BlitsMeClientAppContext.CurrentAppContext.LoginManager.InvalidateSession();
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