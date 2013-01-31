using System;
using System.ComponentModel;
using log4net;

namespace BlitsMe.Agent.Components.Chat
{
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
        public DateTime DateTime { get; set; }
        private bool _lastWord = true;
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

        private void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, e);
        }

        public override string ToString()
        {
            return Message;
        }
    }
}
