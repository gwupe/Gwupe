using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using BlitsMe.Agent.Managers;

namespace BlitsMe.Agent.Components.Notification
{
    class AnswerTrueFalseCommand : ICommand
    {
        public String Message { get; set; }
        private readonly NotificationManager _notificationManager;
        private readonly TrueFalseNotification _notification;

        internal AnswerTrueFalseCommand(NotificationManager manager, TrueFalseNotification notification)
        {
            _notificationManager = manager;
            _notification = notification;
        }
        
        public void Execute(object parameter)
        {
            bool accept = (bool)parameter;
            _notificationManager.DeleteNotification(_notification);
            if(accept)
            {
                _notification.OnAnswerTrue(EventArgs.Empty);
            } else
            {
                _notification.OnAnswerFalse(EventArgs.Empty);
            }
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
}
