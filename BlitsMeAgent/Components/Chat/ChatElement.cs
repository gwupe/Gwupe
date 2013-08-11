using System;
using System.ComponentModel;
using log4net;

namespace BlitsMe.Agent.Components.Chat
{

    public enum ChatDeliveryState
    {
        NotAttempted=1,
        Trying,
        Delivered,
        FailedTrying,
        Failed
    };

    public class ChatElement : INotifyPropertyChanged
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ChatElement));
        private String _message;
        public String Speaker { get; set; }
        public String Message
        {
            get { return _message; }
            set { _message = value; OnPropertyChanged(new PropertyChangedEventArgs("Message")); }
        }
        public DateTime SpeakTime { get; set; }
        public DateTime DeliveryTime
        {
            get { return _deliveryTime; }
            private set
            {
                _deliveryTime = value;
                OnPropertyChanged(new PropertyChangedEventArgs("DeliveryTime"));
            }
        }

        public bool Delivered
        {
            get { return _delivered; }
            private set
            {
                _delivered = value;
                if (_delivered)
                {
                    DeliveryTime = DateTime.Now;
                }
                OnPropertyChanged(new PropertyChangedEventArgs("Delivered"));
            }
        }

        private bool _lastWord = true;
        private DateTime _deliveryTime;
        private bool _delivered;

        public bool LastWord
        {
            get { return _lastWord; }
            set { _lastWord = value; OnPropertyChanged(new PropertyChangedEventArgs("ChatType")); }
        }

        public String ChatType
        {
            get
            {
                if (Speaker.Equals("_SELF"))
                {
                    if (LastWord)
                    {
                        return "ChatMeSingle";
                    }
                    else
                    {
                        return "ChatMeGroup";
                    }
                }
                else if (Speaker.Equals("_SERVICE_COMPLETE"))
                {
                    return "ChatServiceComplete";
                }
                else if (Speaker.Equals("_SYSTEM"))
                {
                    return "ChatSystem";
                }
                else if (Speaker.Equals("_SYSTEM_ERROR"))
                {
                    return "ChatSystemError";
                }
                else
                {
                    if (LastWord)
                    {
                        return "ChatOtherSingle";
                    }
                    else
                    {
                        return "ChatOtherGroup";
                    }
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, e);
        }

        public override string ToString()
        {
            return Message;
        }

        private ChatDeliveryState _deliveryState = ChatDeliveryState.NotAttempted;
        public ChatDeliveryState DeliveryState
        {
            get
            {
                return _deliveryState;
            }
            set
            {
                _deliveryState = value;
                if (_deliveryState == ChatDeliveryState.Delivered)
                    this.Delivered = true;
                OnPropertyChanged(new PropertyChangedEventArgs("DeliveryState"));
            }
        }

    }
}
