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
                TrueFalseNotification notification;
                if (request.team)
                {
                    notification = new JoinTeamNotification()
                    {
                        Manager = _appContext.NotificationManager,
                        Avatar =
                            request.teamElement.hasAvatar
                                ? Convert.FromBase64String(request.teamElement.avatarData)
                                : null,
                        Name = request.teamElement.name,
                        Location = request.teamElement.location,
                        Message = "Join Team?",
                        TeamUsername = request.teamElement.user,
                    };
                }
                else
                {
                    notification = new AddBuddyNotification()
                    {
                        Manager = _appContext.NotificationManager,
                        Avatar =
                            request.userElement.hasAvatar
                                ? Convert.FromBase64String(request.userElement.avatarData)
                                : null,
                        Name = request.userElement.name,
                        Location = request.userElement.location,
                        Message = "Add Contact?",
                    };
                }
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
