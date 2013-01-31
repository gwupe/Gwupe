using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlitsMe.Cloud.Exceptions;
using BlitsMe.Cloud.Messaging.Request;
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

        internal Chat(BlitsMeClientAppContext appContext, Engagement engagement)
        {
            this._appContext = appContext;
            this._engagement = engagement;
            this._to = engagement.SecondParty.Username;
            Conversation = new Conversation();
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

        internal void LogSystemMessage(String message)
        {
            Conversation.AddMessage(message, "_SYSTEM");
            // Fire the event
            OnNewMessage(new ChatEventArgs(_engagement)
                {
                    From = "_SYSTEM",
                    To = _appContext.LoginManager.LoginDetails.username,
                    Message = message
                });
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
                _appContext.ConnectionManager.Connection.Request(new ChatMessageRq()
                    {
                        from =
                            _appContext.LoginManager.LoginDetails.
                                        username,
                        message = message,
                        to = _to,
                        chatId = ChatId,
                    });
                Conversation.AddMessage(message, "_SELF");
                // Fire the event
                OnNewMessage(new ChatEventArgs(_engagement)
                    {
                        From = _appContext.LoginManager.LoginDetails.username,
                        To = _to,
                        Message = message
                    });
            }
            catch (MessageException e)
            {
                Logger.Error("Failed to send change message to " + _to + " : " + e.Message, e);
                this.LogSystemMessage("Message Send Failure");
            }
            catch (Exception e)
            {
                Logger.Error("Failed to send chat message to " + _to + " : " + e.Message, e);
                this.LogSystemMessage("Message Send Failure");
            }
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
