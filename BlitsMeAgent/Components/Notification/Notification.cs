using System;
using System.Windows.Input;
using BlitsMe.Agent.Managers;

namespace BlitsMe.Agent.Components.Notification
{
    internal abstract class Notification
    {
        private ICommand _deleteNotification;
        public NotificationManager Manager { get; set; }
        public event EventHandler Deleted;
        public String From { get; set; }
        public int DeleteTimeout { get; set; }
        public readonly long NotifyTime;

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
        }

        public String Message { get; set; }

        public override string ToString()
        {
            return this.GetType() + " : " + Message;
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
