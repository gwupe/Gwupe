using System;
using BlitsMe.Cloud.Messaging.API;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using log4net;

namespace BlitsMe.Agent.Components.Processors
{
    internal class NotifyChangeProcessor : Processor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (NotifyChangeProcessor));

        public NotifyChangeProcessor(BlitsMeClientAppContext appContext)
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
                        BlitsMeClientAppContext.CurrentAppContext.RosterManager.RequestContactUpdate(request.changeId);
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