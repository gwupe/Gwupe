using System;
using System.ComponentModel;
using System.Windows.Input;
using BlitsMe.Common.Security;
using log4net;

namespace BlitsMe.Agent.Components.Functions.Chat.ChatElement
{



    public abstract class ChatElement : INotifyPropertyChanged, IChatMessage
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ChatElement));
        //public ChatElementManager Manager { get; set; }
        private String _message;
        public abstract string Speaker { get; set; }

        public String Message
        {
            get { return _message; }
            set { _message = value; OnPropertyChanged("Message"); }
        }
        public DateTime SpeakTime { get; set; }

        private bool _lastWord = true;
        private string _userName = string.Empty;

        public bool LastWord
        {
            get { return _lastWord; }
            set { _lastWord = value; OnPropertyChanged("ChatType"); }
        }

        public string UserName
        {
            get { return _userName; }
            set { _userName = value; OnPropertyChanged("UserName"); }
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
                    if (LastWord)
                    {
                        return "ChatSystem";
                    }
                    else
                    {
                        return "ChatSystemGroup";
                    }
                }
                else if (Speaker.Equals("_SECONDPARTYSYSTEM"))
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
                else if (Speaker.Equals("_SYSTEM_ERROR"))
                {
                    return "ChatSystemError";
                }

                else if (Speaker.Equals("_FILE_SEND_REQUEST"))
                {
                    if (LastWord)
                    {
                        return "ChatNotification";
                    }
                    else
                    {
                        return "ChatNotificationGroup";
                    }

                }
                else if (Speaker.Equals("_UNATTENDED_RDP_REQUEST"))
                {
                    return "RDPRequestUnattendedNotification";
                }
                else if (Speaker.Equals("_RDP_REQUEST"))
                {
                    return "RDPRequestNotification";
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

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }


        public override string ToString()
        {
            return SpeakTime + ": " + Speaker + "> " + Message;
        }


        /*
        private ICommand _deleteNotification;
        public event EventHandler Deleted;
        public String AssociatedUsername { get; set; }
        public int DeleteTimeout { get; set; }
        internal String Id { get; set; }
        public readonly long NotifyTime;
        */
        private byte[] _person;
        private string _name;
        private string _location;
        private string _flag;
        /*
                public void OnProcessDeleteCommand(EventArgs e)
                {
                    EventHandler handler = Deleted;
                    if (handler != null) handler(this, e);
                }

                public ICommand DeleteNotification
                {
                    get { return _deleteNotification ?? (_deleteNotification = new DeleteNotificationCommand(this)); }
                }
        */
        /*        internal ChatElement()
                {
        //            NotifyTime = DateTime.Now.Ticks;
        //            DeleteTimeout = 0;
        //            Id = Util.getSingleton().generateString(32);
                }
                */
        /*
        public virtual byte[] Person
        {
            get { return _person; }
            set { _person = value; OnPropertyChanged("Person"); }
        }

        public virtual string Name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged("Name"); }
        }

        public virtual string Location
        {
            get { return _location; }
            set { _location = value; OnPropertyChanged("Location"); }
        }

        public virtual string Flag
        {
            get { return _flag; }
            set { _flag = value; OnPropertyChanged("Flag"); }
        }

        */

    }
}