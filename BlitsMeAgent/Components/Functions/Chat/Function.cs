using System;
using System.Collections.Concurrent;
using System.Threading;
using BlitsMe.Agent.Components.Functions.API;
using BlitsMe.Cloud.Messaging.API;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using BlitsMe.Common.Security;
using log4net;
using Timer = System.Timers.Timer;

namespace BlitsMe.Agent.Components.Functions.Chat
{
    //internal delegate void ChatEvent(object sender, ChatActivity args);

    internal class Function : FunctionImpl
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Function));

        // Event to be fired if a message is sent or received.
        //internal event ChatEvent NewMessage;
        public override String Name { get { return "Chat"; } }

        // Our app context
        private readonly BlitsMeClientAppContext _appContext;
        // Our engagement
        private readonly Engagement _engagement;
        // The actual conversation
        public Conversation Conversation;
        // Who we are talking to
        private readonly String _to;
        // The thread id
        private String _threadId;

        // Thread which handles the sending of messages (sending is async)
        private readonly Thread _chatSender;
        private readonly ConcurrentQueue<ChatElement> _chatQueue;

        internal Function(BlitsMeClientAppContext appContext, Engagement engagement)
        {
            this._appContext = appContext;
            this._engagement = engagement;
            this._to = engagement.SecondParty.Person.Username;
            Conversation = new Conversation(appContext);
            _chatQueue = new ConcurrentQueue<ChatElement>();
            _chatSender = new Thread(ProcessChats) { Name = "ChatSender-" + _to, IsBackground = true };
            _chatSender.Start();
        }

        public override void Close()
        {
            if (_chatSender != null && _chatSender.IsAlive)
            {
                _chatSender.Abort();
            }
        }

        private void ProcessChats()
        {
            while (true)
            {
                while (_chatQueue.Count > 0)
                {
                    ChatElement chatElement;
                    if (_chatQueue.TryPeek(out chatElement))
                    {
                        chatElement.DeliveryState = ChatDeliveryState.Trying;
                        var chatMessageRq = new ChatMessageRq()
                            {
                                message = chatElement.Message,
                                username = _to,
                                interactionId = _engagement.Interactions.CurrentOrNewInteraction.Id,
                                chatId = _threadId ?? (_threadId = Util.getSingleton().generateString(6))
                            };
                        try
                        {
                            Response response = _appContext.ConnectionManager.Connection.Request<ChatMessageRq, ChatMessageRs>(chatMessageRq);
                            chatElement.DeliveryState = ChatDeliveryState.Delivered;
                            _chatQueue.TryDequeue(out chatElement);
                        }
                        catch (Exception e)
                        {
                            Logger.Error("Failed to send chat message to " + _to + " : " + e.Message, e);
                            // Set all pending to still trying
                            foreach (ChatElement element in _chatQueue.ToArray())
                            {
                                element.DeliveryState = ChatDeliveryState.FailedTrying;
                            }
                            Thread.Sleep(1000);
                        }
                    }
                    else
                    {
                        Logger.Error("Failed to peek into the chat queue, cannot process message");
                        // Failed to dequeue, wait a second
                        Thread.Sleep(1000);
                    }
                }
                lock (_chatQueue)
                {
                    while (_chatQueue.Count == 0)
                    {
                        Monitor.Wait(_chatQueue);
                    }
                }
            }
        }

        internal void ReceiveChatMessage(String message, String chatId, String interactionId, String shortCode)
        {
            Conversation.AddMessage(message, _to);
            if (chatId != null)
            {
                _threadId = chatId;
            }
            // Fire the event
            OnActivate(EventArgs.Empty);
            OnNewActivity(new ChatActivity(_engagement, ChatActivity.CHAT_RECEIVE)
                             {
                                 From = _to,
                                 To = _appContext.CurrentUserManager.CurrentUser.Username,
                                 Message = message
                             });
            OnDeactivate(EventArgs.Empty);
        }

        internal ChatElement LogSystemMessage(String message)
        {
            ChatElement chatElement = Conversation.AddMessage(message, "_SYSTEM");
            // Fire the event
            OnNewActivity(new ChatActivity(_engagement, ChatActivity.LOG_SYSTEM)
                {
                    From = "_SYSTEM",
                    To = _appContext.CurrentUserManager.CurrentUser.Username,
                    Message = message
                });
            return chatElement;
        }

        internal void LogServiceCompleteMessage(String message)
        {
            Conversation.AddMessage(new ServiceCompleteChatElement(_engagement)
            {
                Message = message,
                Speaker = "_SERVICE_COMPLETE",
                DeliveryState = ChatDeliveryState.Delivered,
                SpeakTime = DateTime.Now
            });
            // Fire the event
            OnNewActivity(new ChatActivity(_engagement, ChatActivity.LOG_SERVICE)
            {
                From = "_SERVICE_COMPLETE",
                To = _appContext.CurrentUserManager.CurrentUser.Username,
                Message = message
            });
        }

        internal void SendChatMessage(String message)
        {
            if (ParseSystemCommand(message)) return;
            OnActivate(EventArgs.Empty);
            try
            {
                var chatElement = new ChatElement() { Message = message, Speaker = "_SELF", SpeakTime = DateTime.Now };
                lock (_chatQueue)
                {
                    _chatQueue.Enqueue(chatElement);
                    Monitor.PulseAll(_chatQueue);
                }
                Conversation.AddMessage(chatElement);
                // Fire the event
                OnNewActivity(new ChatActivity(_engagement, ChatActivity.CHAT_SEND)
                {
                    From = _appContext.CurrentUserManager.CurrentUser.Username,
                    To = _to,
                    Message = message
                });
            }
            catch (Exception e)
            {
                Logger.Error("Failed to send chat message to " + _to + " : " + e.Message, e);
                this.LogSystemMessage("Message Send Failure");
            }
            finally
            {
                OnDeactivate(EventArgs.Empty);
            }
        }

        private bool ParseSystemCommand(String message)
        {
            if ("/EnableDebug".Equals(message))
            {
                _appContext.Debug = true;
                return true;
            }
            if ("/DisableDebug".Equals(message))
            {
                _appContext.Debug = false;
                return true;
            }
            return false;
        }

        private void ChatResponseCallback(Response obj)
        {
            Logger.Error("Not a real error, just notice this.");
        }
        /*
                internal void OnNewMessage(ChatActivity args)
                {
                    ChatEvent handler = NewMessage;
                    if (handler != null) handler(this, args);
                }
        */
        public ChatElement LogErrorMessage(string message)
        {
            ChatElement chatElement = Conversation.AddMessage(message, "_SYSTEM_ERROR");
            // Fire the event
            OnNewActivity(new ChatActivity(_engagement, ChatActivity.LOG_ERROR)
            {
                From = "_SYSTEM_ERROR",
                To = _appContext.CurrentUserManager.CurrentUser.Username,
                Message = message
            });
            return chatElement;
        }
    }

    internal class ChatActivity : EngagementActivity
    {
        internal const String CHAT_SEND = "CHAT_REQUEST";
        internal const String LOG_ERROR = "LOG_ERROR";
        internal const String LOG_SYSTEM = "LOG_SYSTEM";
        internal const String CHAT_RECEIVE = "CHAT_RECEIVE";
        internal const String LOG_SERVICE = "LOG_SERVICE";
        internal String Message;

        internal ChatActivity(Engagement engagement, String activity)
            : base(engagement, "CHAT", activity)
        {
        }
    }

}
