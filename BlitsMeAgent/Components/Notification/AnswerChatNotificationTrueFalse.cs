using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Input;
using BlitsMe.Agent.Components.Functions.Chat;
using BlitsMe.Agent.Managers;

namespace BlitsMe.Agent.Components.Notification
{
    class AnswerChatNotificationTrueFalse : ICommand
    {
        public String Message { get; set; }
        //private readonly ChatElementManager _notificationManager;
        private readonly ChatNotificationTrueFalse _notification;

        internal AnswerChatNotificationTrueFalse(ChatNotificationTrueFalse notification)
        {
            //_notificationManager = manager;
            _notification = notification;
        }
        
        public void Execute(object parameter)
        {
            bool accept = (bool)parameter;
            Thread execThread;
            //_notificationManager.DeleteNotification(_notification);
            if(accept)
            {
                execThread = new Thread(() => _notification.OnAnswerTrue(EventArgs.Empty));
            } else
            {
                execThread = new Thread(() => _notification.OnAnswerFalse(EventArgs.Empty));
            }
            execThread.IsBackground = true;
            execThread.Start();
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
}
