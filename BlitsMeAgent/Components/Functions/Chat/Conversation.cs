using System;
using System.Collections.ObjectModel;
using log4net;

namespace BlitsMe.Agent.Components.Functions.Chat
{
    public class Conversation
    {
        private readonly BlitsMeClientAppContext _appContext;
        private static readonly ILog Logger = LogManager.GetLogger(typeof (Conversation));
        public ObservableCollection<ChatElement> Exchange{ get; private set; }
        public DateTime Started { get; set; }
        public DateTime Stopped { get; set; }
        // 10 minute conversation groups
        private const Int64 MaxChatInterval = 6000000000;

        public Conversation(BlitsMeClientAppContext appContext)
        {
            _appContext = appContext;
            Exchange = new ObservableCollection<ChatElement>();
        }

        public ChatElement AddMessage(String message, String speaker, string username)
        {
            ChatElement newMessage = new ChatElement
                {
                    Message = message, 
                    Speaker = speaker, 
                    DeliveryState = ChatDeliveryState.Delivered, 
                    SpeakTime = DateTime.Now,
                    AssociatedUsername = username
                };
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

                if (lastMessage.Speaker.Equals(newMessage.AssociatedUsername) &&
                        (newMessage.ChatType == "ChatNotification" || 
                        newMessage.ChatType == "RDPRequestNotification"))
                {
                    lastMessage.LastWord = false;
                }

                if (lastMessage.AssociatedUsername != null)
                {
                    if (lastMessage.AssociatedUsername.Equals(newMessage.AssociatedUsername) &&
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
