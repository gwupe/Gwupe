using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Windows.Input;
using BlitsMe.Agent.Components.Notification;
using BlitsMe.Agent.Managers;
using BlitsMe.Common.Security;
using log4net;

namespace BlitsMe.Agent.Components.Functions.Chat
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
        public ChatElementManager Manager { get; set; }
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
                    if (LastWord)
                    {
                        return "ChatSystem";
                    }
                    else
                    {
                        return "ChatMeGroup";
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

                else if (Speaker.Equals("_NOTIFICATION_CHAT"))
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

        private ICommand _deleteNotification;
        public event EventHandler Deleted;
        public String AssociatedUsername { get; set; }
        public int DeleteTimeout { get; set; }
        internal String Id { get; set; }
        public readonly long NotifyTime;
        private byte[] _person;
        private string _name;
        private string _location;
        private string _flag;

        public void OnProcessDeleteCommand(EventArgs e)
        {
            EventHandler handler = Deleted;
            if (handler != null) handler(this, e);
        }

        public ICommand DeleteNotification
        {
            get { return _deleteNotification ?? (_deleteNotification = new DeleteNotificationCommand(Manager, this)); }
        }

        internal ChatElement()
        {
            NotifyTime = DateTime.Now.Ticks;
            DeleteTimeout = 0;
            Id = Util.getSingleton().generateString(32);
        }

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



        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    internal class DeleteNotificationCommand : ICommand
    {
        private readonly ChatElementManager _notificationManager;
        private readonly ChatElement _notification;

        internal DeleteNotificationCommand(ChatElementManager manager, ChatElement notification)
        {
            this._notificationManager = manager;
            this._notification = notification;
        }

        public void Execute(object parameter)
        {
            // Remove from the list
            _notificationManager.DeleteNotification(_notification);
            // Process any event handlers linked to this
            _notification.OnProcessDeleteCommand(new EventArgs());
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;

    }
}
