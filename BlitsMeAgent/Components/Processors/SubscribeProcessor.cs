using System;
using System.Text;
using Gwupe.Agent.Components.Notification;
using Gwupe.Cloud.Messaging.API;
using Gwupe.Cloud.Messaging.Request;
using Gwupe.Cloud.Messaging.Response;
using log4net;

namespace Gwupe.Agent.Components.Processors
{
    class SubscribeProcessor : Processor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SubscribeProcessor));
        private readonly GwupeClientAppContext _appContext;

        public SubscribeProcessor(GwupeClientAppContext appContext)
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
                {
                    Manager = _appContext.NotificationManager,
                    Person = request.userElement.hasAvatar ? Convert.FromBase64String(request.userElement.avatarData) :  null,
                    Name = request.userElement.name,
                    Location = request.userElement.location,
                    Message = request.userElement.name + " would like to add you."
                };
                notification.AnswerHandler.Answered += delegate { ProcessAnswer(notification.AnswerHandler.Answer, request.username); };
                _appContext.NotificationManager.AddNotification(notification);
                _appContext.UIManager.Show();
            }
            return response;
        }

        private void ProcessAnswer(bool answer, String username)
        {
            if (answer)
            {
                SubscribeRq request = new SubscribeRq { subscribe = true, username = username };
                _appContext.ConnectionManager.Connection.RequestAsync<SubscribeRq, SubscribeRs>(request, SubscribeRequestResponseHandler);
            }
            else
            {
                SubscribeRq request = new SubscribeRq { subscribe = false, username = username };
                _appContext.ConnectionManager.Connection.RequestAsync<SubscribeRq, SubscribeRs>(request, SubscribeRequestResponseHandler);
            }
        }

        private void SubscribeRequestResponseHandler(SubscribeRq request, SubscribeRs response, Exception e)
        {
            if (e != null)
            {
                Logger.Error("Failed to send subscribe answer to " + request.username + " : " +
                             e.Message, e);
            }
        }
    }
}
