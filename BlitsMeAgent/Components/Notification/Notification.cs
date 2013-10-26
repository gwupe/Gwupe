using System;
using System.ComponentModel;
using System.Windows.Input;
using BlitsMe.Agent.Managers;
using BlitsMe.Common.Security;

namespace BlitsMe.Agent.Components.Notification
{
    internal abstract class Notification : INotifyPropertyChanged
    {
        private ICommand _deleteNotification;
        public NotificationManager Manager { get; set; }
        public event EventHandler Deleted;
        public String AssociatedUsername { get; set; }
        public int DeleteTimeout { get; set; }
        internal String Id { get; set; }
        public readonly long NotifyTime;
        private string _message;
        private byte[] _person;
        private string _name;
        private string _location;

        public void OnProcessDeleteCommand(EventArgs e)
        {
            EventHandler handler = Deleted;
            if (handler != null) handler(this, e);
        }

        public ICommand DeleteNotification
        {
            get { return _deleteNotification ?? (_deleteNotification = new DeleteNotificationCommand(Manager, this)); }
        }

        internal Notification()
        {
            NotifyTime = DateTime.Now.Ticks;
            DeleteTimeout = 0;
            Id = Util.getSingleton().generateString(32);
        }

        public virtual String Message
        {
            get { return _message; }
            set { _message = value; OnPropertyChanged("Message"); }
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

        public override string ToString()
        {
            return this.GetType() + " : " + Message;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    internal class DeleteNotificationCommand : ICommand
    {
        private readonly NotificationManager _notificationManager;
        private readonly Notification _notification;

        internal DeleteNotificationCommand(NotificationManager manager, Notification notification)
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
