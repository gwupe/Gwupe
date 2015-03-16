using System;
using System.Collections.ObjectModel;
using Gwupe.Agent.Components.Functions.Chat.ChatElement;
using log4net;

namespace Gwupe.Agent.Components.Functions.Chat
{
    public class Conversation
    {
        private readonly GwupeClientAppContext _appContext;
        private static readonly ILog Logger = LogManager.GetLogger(typeof (Conversation));
        public ObservableCollection<IChatMessage> Exchange { get; private set; }
        public DateTime Started { get; set; }
        public DateTime Stopped { get; set; }
        // 10 minute conversation groups
        private const Int64 MaxChatInterval = 6000000000;

        public Conversation(GwupeClientAppContext appContext)
        {
            _appContext = appContext;
            Exchange = new ObservableCollection<IChatMessage>();
        }

        public BaseChatElement ReceiveMessage(String message, String speaker)
        {
            BaseChatElement newMessage = new TargetChatElement()
            {
                Message = message,
                Speaker = speaker,
                SpeakTime = DateTime.Now
            };
            _addToExchange(newMessage);
            return newMessage;
        }

        public void AddMessage(BaseChatElement element)
        {
            _addToExchange(element);
        }

        private void _addToExchange(IChatMessage newMessage)
        {
            if (Exchange.Count > 0)
            {
                IChatMessage lastMessage = Exchange[Exchange.Count - 1];
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
