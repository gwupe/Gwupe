using System;
using BlitsMe.Agent.Components.Notification;
using BlitsMe.Cloud.Messaging.API;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using log4net;

namespace BlitsMe.Agent.Components.Processors
{
    class SubscribeProcessor : Processor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (SubscribeProcessor));
        private readonly BlitsMeClientAppContext _appContext;

        public SubscribeProcessor(BlitsMeClientAppContext appContext)
        {
            _appContext = appContext;
        }

        public Response process(Request req)
        {
            SubscribeRq request = (SubscribeRq)req;
            SubscribeRs response = new SubscribeRs();
            if (request.subscribe)
            {
                AddBuddyNotification notification = new AddBuddyNotification()
                    {Manager = _appContext.NotificationManager, Message = request.userElement.name + " would like to add you."};
                notification.AnsweredTrue += delegate { ProcessAnswer(true, request.username); };
                notification.AnsweredFalse += delegate { ProcessAnswer(false, request.username); };
                _appContext.NotificationManager.AddNotification(notification);
                _appContext.ShowDashboard();
            }
            return response;
        }

        private void ProcessAnswer(bool answer, String username)
        {
            if(answer)
            {
                SubscribeRq request = new SubscribeRq {subscribe = true, username = username};
                _appContext.ConnectionManager.Connection.RequestAsync<SubscribeRq,SubscribeRs>(request, SubscribeRequestResponseHandler);
            } else
            {
                SubscribeRq request = new SubscribeRq { subscribe = false, username = username };
                _appContext.ConnectionManager.Connection.RequestAsync<SubscribeRq, SubscribeRs>(request, SubscribeRequestResponseHandler);
            }
        }

        private void SubscribeRequestResponseHandler(SubscribeRq request, SubscribeRs response, Exception e)
        {
            if(e != null)
            {
                Logger.Error("Failed to send subscribe answer to " + request.username + " : " +
                             e.Message,e);
            }
        }
    }
}
