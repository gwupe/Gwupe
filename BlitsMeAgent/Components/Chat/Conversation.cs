using System;
using System.Collections.ObjectModel;
using log4net;

namespace BlitsMe.Agent.Components.Chat
{
    public class Conversation
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (Conversation));
        public ObservableCollection<ChatElement> Exchange{ get; set; }
        public DateTime Started { get; set; }
        public DateTime Stopped { get; set; }
        // 10 minute conversation groups
        private const Int64 MaxChatInterval = 6000000000;

        public Conversation()
        {
            Exchange = new ObservableCollection<ChatElement>();
        }

        public ChatElement AddMessage(String message, String speaker)
        {
            ChatElement newMessage = new ChatElement {Message = message, Speaker = speaker, DeliveryState = ChatDeliveryState.Delivered, SpeakTime = DateTime.Now };
            _addToExchange(newMessage);
            return newMessage;
        }

        public void AddMessage(ChatElement element)
        {
            _addToExchange(element);
        }

        private void _addToExchange(ChatElement newMessage)
        {
            if (Exchange.Count > 0)
            {
                ChatElement lastMessage = Exchange[Exchange.Count - 1];
                if (lastMessage.Speaker.Equals(newMessage.Speaker) &&
                    (newMessage.SpeakTime.Ticks - lastMessage.SpeakTime.Ticks < MaxChatInterval))
                {
                    lastMessage.LastWord = false;
                }
            }
            Exchange.Add(newMessage);
        }
    }
}
