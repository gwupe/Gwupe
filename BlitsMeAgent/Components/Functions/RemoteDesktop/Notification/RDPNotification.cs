using System;
using System.Windows.Input;
using BlitsMe.Agent.Components.Notification;
using BlitsMe.Agent.Managers;

namespace BlitsMe.Agent.Components.Functions.RemoteDesktop.Notification
{
    class RDPNotification : Components.Notification.Notification
    {
        public event EventHandler ProcessAcceptRDP;

        public void OnProcessAcceptRDP(EventArgs e)
        {
            EventHandler handler = ProcessAcceptRDP;
            if (handler != null) handler(this, e);
        }

        public event EventHandler ProcessDenyRDP;

        public void OnProcessDenyRDP(EventArgs e)
        {
            EventHandler handler = ProcessDenyRDP;
            if (handler != null) handler(this, e);
        }

        private ICommand _answerRDP;
        public ICommand AnswerRDP
        {
            get { return _answerRDP ?? (_answerRDP = new AnswerRDPRequestCommand(this.Manager, this)); }
        }

        internal RDPNotification()
        {
            // timesout after .... seconds
            DeleteTimeout = 300;
        }
    }

    class AnswerRDPRequestCommand : ICommand
    {
        private readonly NotificationManager _notificationManager;
        private readonly RDPNotification _notification;

        internal AnswerRDPRequestCommand(NotificationManager manager, RDPNotification notification)
        {
            this._notificationManager = manager;
            this._notification = notification;
        }
        
        public void Execute(object parameter)
        {
            bool accept = (bool)parameter;
            _notificationManager.DeleteNotification(_notification);
            if(accept)
            {
                _notification.OnProcessAcceptRDP(new EventArgs());
            } else
            {
                _notification.OnProcessDenyRDP(new EventArgs());
            }
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
}
