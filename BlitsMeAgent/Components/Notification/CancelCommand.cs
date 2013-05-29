using System;
using System.Threading;
using System.Windows.Input;
using BlitsMe.Agent.Managers;

namespace BlitsMe.Agent.Components.Notification
{
    class CancelCommand : ICommand
    {
        private readonly NotificationManager _notificationManager;
        private readonly CancellableNotification _notification;

        internal CancelCommand(NotificationManager manager, CancellableNotification notification)
        {
            _notificationManager = manager;
            _notification = notification;
        }

        public void Execute(object parameter)
        {
            _notificationManager.DeleteNotification(_notification);
            var execThread = new Thread(() => _notification.OnCancel(EventArgs.Empty)) {IsBackground = true};
            execThread.Start();
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
}
