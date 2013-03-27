using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using BlitsMe.Cloud.Exceptions;
using BlitsMe.Cloud.Messaging.API;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using log4net;

namespace BlitsMe.Agent.Components.Chat
{
    internal delegate void ChatEvent(object sender, ChatEventArgs args);

    internal class Chat
    {
        // Event to be fired if a message is sent or received.
        internal event ChatEvent NewMessage;

        // Our app context
        private readonly BlitsMeClientAppContext _appContext;
        // Our engagement
        private readonly Engagement _engagement;
        // Our logger
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Chat));
        // The actual conversation
        public Conversation Conversation;
        // Who we are talking to
        private readonly String _to;
        // the XMPP chat id
        public String ChatId;

        // Thread which handles the sending of messages (sending is async)
        private readonly Thread _chatSender;
        private ConcurrentQueue<ChatElement> _chatQueue;

        internal Chat(BlitsMeClientAppContext appContext, Engagement engagement)
        {
            this._appContext = appContext;
            this._engagement = engagement;
            this._to = engagement.SecondParty.Username;
            Conversation = new Conversation();
            _chatQueue = new ConcurrentQueue<ChatElement>();
            _chatSender = new Thread(ProcessChats) {Name = "ChatSender-" + _to, IsBackground = true};
            _chatSender.Start();
        }

        internal void Close()
        {
            if(_chatSender != null && _chatSender.IsAlive)
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
                                shortCode = _appContext.LoginManager.LoginDetails.shortCode,
                                chatId = ChatId,
                            };
                        try
                        {
                            Response response = _appContext.ConnectionManager.Connection.Request<ChatMessageRq,ChatMessageRs>(chatMessageRq);
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
                    } else
                    {
                        Logger.Error("Failed to peek into the chat queue, cannot process message");
                        // Failed to dequeue, wait a second
                        Thread.Sleep(1000);
                    }
                }
                lock(_chatQueue)
                {
                    while (_chatQueue.Count == 0)
                    {
#if DEBUG
                        Logger.Debug("Listening for new messages");
#endif
                        Monitor.Wait(_chatQueue);
                    }
                }
            }
        }

        internal void ReceiveChatMessage(String message, String chatId, String shortCode)
        {
            _engagement.SecondParty.ShortCode = shortCode;
            Conversation.AddMessage(message, _to);
            // Fire the event
            OnNewMessage(new ChatEventArgs(_engagement)
                             {
                                 From = _to,
                                 To = _appContext.LoginManager.LoginDetails.username,
                                 Message = message
                             });

        }

        internal ChatElement LogSystemMessage(String message)
        {
            ChatElement chatElement = Conversation.AddMessage(message, "_SYSTEM");
            // Fire the event
            OnNewMessage(new ChatEventArgs(_engagement)
                {
                    From = "_SYSTEM",
                    To = _appContext.LoginManager.LoginDetails.username,
                    Message = message
                });
            return chatElement;
        }

        internal void LogServiceCompleteMessage(String message)
        {
            Conversation.AddMessage(message, "_SERVICE_COMPLETE");
            // Fire the event
            OnNewMessage(new ChatEventArgs(_engagement)
            {
                From = "_SERVICE_COMPLETE",
                To = _appContext.LoginManager.LoginDetails.username,
                Message = message
            });
        }

        internal void SendChatMessage(String message)
        {
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
                OnNewMessage(new ChatEventArgs(_engagement)
                    {
                        From = _appContext.LoginManager.LoginDetails.username,
                        To = _to,
                        Message = message
                    });
            }
            catch (Exception e)
            {
                Logger.Error("Failed to send chat message to " + _to + " : " + e.Message, e);
                this.LogSystemMessage("Message Send Failure");
            }
        }

        private void chatResponseCallback(Response obj)
        {
            Logger.Error("Not a real error, just notice this.");
        }

        internal void OnNewMessage(ChatEventArgs args)
        {
            ChatEvent handler = NewMessage;
            if (handler != null) handler(this, args);
        }
    }

    internal class ChatEventArgs : EngagementActivityArgs
    {

        public String Message { get; set; }
        public override string ActivityType
        {
            get { return "CHAT"; }
        }

        internal ChatEventArgs(Engagement engagement)
            : base(engagement)
        {

        }
    }
}
