using System;
using System.Windows.Input;
using BlitsMe.Agent.Components.Notification;
using BlitsMe.Agent.Managers;

namespace BlitsMe.Agent.Components.Functions.FileSend.Notification
{
    internal delegate void FileSendEventHandler(object sender, FileSendEventArgs args);

    class FileSendRequestNotification : INotification
    {
        internal FileSendInfo FileInfo;

        public event FileSendEventHandler ProcessAcceptFile;

        public void OnProcessFileResponse(bool accept)
        {
            var e = new FileSendEventArgs() { FileInfo = FileInfo };
            FileSendEventHandler handler = accept ? ProcessAcceptFile : ProcessDenyFile;
            if (handler != null) handler(this, e);
        }

        public event FileSendEventHandler ProcessDenyFile;

        private ICommand _answerFileSendRequest;
        public ICommand AnswerFileSendRequest
        {
            get { return _answerFileSendRequest ?? (_answerFileSendRequest = new AnswerFileSendRequestCommand(this.Manager, this)); }
        }

        internal class AnswerFileSendRequestCommand : ICommand
        {
            private readonly NotificationManager _notificationManager;
            private readonly FileSendRequestNotification _fileSendRequestNotification;

            public AnswerFileSendRequestCommand(NotificationManager notificationManager, FileSendRequestNotification fileSendRequestNotification)
            {
                _notificationManager = notificationManager;
                _fileSendRequestNotification = fileSendRequestNotification;
            }

            public void Execute(object parameter)
            {
                bool accept = (bool)parameter;
                _notificationManager.DeleteNotification(_fileSendRequestNotification);
                _fileSendRequestNotification.OnProcessFileResponse(accept);
            }

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged;
        }

        internal FileSendRequestNotification()
        {
            DeleteTimeout = 300;
        }
    }

}
