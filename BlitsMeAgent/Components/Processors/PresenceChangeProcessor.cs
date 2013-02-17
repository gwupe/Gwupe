using System;
using BlitsMe.Agent.Components.Person;
using BlitsMe.Cloud.Messaging.API;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using log4net;
using log4net.Repository.Hierarchy;

namespace BlitsMe.Agent.Components.Processors
{
    public class PresenceChangeProcessor : Processor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (PresenceChangeProcessor));

        private readonly BlitsMeClientAppContext _appContext;

        public PresenceChangeProcessor(BlitsMeClientAppContext appContext)
        {
            _appContext = appContext;
        }

        public Response process(Request req)
        {
            var request = (PresenceChangeRq) req;
            var response = new PresenceChangeRs();
            try
            {
                Person.Person servicePerson = _appContext.RosterManager.GetServicePerson(request.user);
                if (servicePerson != null)
                {
                    servicePerson.Presence = new Presence(request.presence);
                    Logger.Info("Presence change, " + request.user +
                                (servicePerson.Presence.IsAvailable ? " is available " : " is no longer available"));
                    if (request.shortCode != null)
                    {
                        servicePerson.ShortCode = request.shortCode;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to process presence change : " + e.Message);
                response.error = "UNKNOWN_ERROR";
                response.errorMessage = "Failed to process presence change";
            }
            return response;


        }
    }
}