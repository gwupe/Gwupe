using System;
using Gwupe.Cloud.Messaging.API;
using Gwupe.Cloud.Messaging.Request;
using Gwupe.Cloud.Messaging.Response;
using log4net;

namespace Gwupe.Agent.Components.Processors
{
    internal class NotifyChangeProcessor : Processor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (NotifyChangeProcessor));

        public NotifyChangeProcessor(GwupeClientAppContext appContext)
        {
        }

        public Response process(Request rq)
        {
            NotifyChangeRq request = (NotifyChangeRq) rq;
            NotifyChangeRs response = new NotifyChangeRs();
            if (request.changeObject.Equals(NotifyChangeRq.OBJECT_TYPE_USER))
            {
                if (request.changeType.Equals(NotifyChangeRq.CHANGE_TYPE_MOD))
                {
                    try
                    {
                        GwupeClientAppContext.CurrentAppContext.PartyManager.GetParty(request.changeId, true);
                    }
                    catch (Exception)
                    {
                        Logger.Error("Failed to request a contact update for " + request.changeId);
                    }
                }
            }
            return response;
        }
    }
}