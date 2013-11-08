using System;
using System.Collections.ObjectModel;
using BlitsMe.Agent.Components.Functions.Chat.ChatElement;
using log4net;

namespace BlitsMe.Agent.Components.Functions.Chat
{
    public class Conversation
    {
        private readonly BlitsMeClientAppContext _appContext;
        private static readonly ILog Logger = LogManager.GetLogger(typeof (Conversation));
        public ObservableCollection<ChatElement.ChatElement> Exchange{ get; private set; }
        public DateTime Started { get; set; }
        public DateTime Stopped { get; set; }
        // 10 minute conversation groups
        private const Int64 MaxChatInterval = 6000000000;

        public Conversation(BlitsMeClientAppContext appContext)
        {
            _appContext = appContext;
            Exchange = new ObservableCollection<ChatElement.ChatElement>();
        }

        public ChatElement.ChatElement ReceiveMessage(String message, String speaker)
        {
            ChatElement.ChatElement newMessage = new TargetChatElement()
            {
                Message = message,
                Speaker = speaker,
                SpeakTime = DateTime.Now
            };
            _addToExchange(newMessage);
            return newMessage;
        }

        public void AddMessage(ChatElement.ChatElement element)
        {
            _addToExchange(element);
        }

        private void _addToExchange(ChatElement.ChatElement newMessage)
        {
            if (Exchange.Count > 0)
            {
                ChatElement.ChatElement lastMessage = Exchange[Exchange.Count - 1];
                if (lastMessage.Speaker.Equals(newMessage.Speaker) &&
                    (newMessage.SpeakTime.Ticks - lastMessage.SpeakTime.Ticks < MaxChatInterval))
                {
                    lastMessage.LastWord = false;
                }

                if (lastMessage.UserName.Equals(newMessage.UserName) &&
                        (newMessage.ChatType == "ChatNotification" ||
                        newMessage.ChatType == "RDPRequestNotification"))
                {
                    lastMessage.LastWord = false;
                }

                if (lastMessage.UserName != null)
                {
                    if (lastMessage.UserName.Equals(newMessage.UserName) &&
                        (lastMessage.ChatType == "ChatNotification" ||
                         lastMessage.ChatType == "RDPRequestNotification"))
                    {
                        lastMessage.LastWord = false;
                    }
                }
            }
            Exchange.Add(newMessage);
        }
    }
}
