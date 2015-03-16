using System;
using Gwupe.Agent.Components.Person;
using Gwupe.Cloud.Messaging.API;
using Gwupe.Cloud.Messaging.Request;
using Gwupe.Cloud.Messaging.Response;
using log4net;
using log4net.Repository.Hierarchy;

namespace Gwupe.Agent.Components.Processors
{
    public class PresenceChangeProcessor : Processor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (PresenceChangeProcessor));

        private readonly GwupeClientAppContext _appContext;

        public PresenceChangeProcessor(GwupeClientAppContext appContext)
        {
            _appContext = appContext;
        }

        public Response process(Request req)
        {
            var request = (PresenceChangeRq) req;
            var response = new PresenceChangeRs();
            try
            {
                _appContext.RosterManager.ProcessPresenceChange(request);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to process presence change : " + e.Message,e);
                response.error = "UNKNOWN_ERROR";
                response.errorMessage = "Failed to process presence change";
            }
            return response;


        }
    }
}