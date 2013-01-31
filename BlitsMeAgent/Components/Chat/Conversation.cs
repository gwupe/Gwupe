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

        public void AddMessage(String message, String speaker)
        {
            ChatElement newMessage = new ChatElement {Message = message, DateTime = DateTime.Now, Speaker = speaker};
            if(Exchange.Count > 0)
            {
                ChatElement lastMessage = Exchange[Exchange.Count - 1];
                if (lastMessage.Speaker.Equals(speaker) && (newMessage.DateTime.Ticks - lastMessage.DateTime.Ticks < MaxChatInterval))
                {
                    lastMessage.LastWord = false;
                }
            }
            Exchange.Add(newMessage);
        }
    }
}
