using System;
using System.ComponentModel;
using System.Windows.Input;
using BlitsMe.Agent.Components.Notification;
using BlitsMe.Agent.Managers;
using log4net;

namespace BlitsMe.Agent.Components.Functions.FileSend.Notification
{
    class FileSendProgressNotification : INotification, INotifyPropertyChanged
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (FileSendProgressNotification));
        public FileSendInfo FileInfo;

        public int Progress
        {
            get { return _progress; }
            set { _progress = value; OnPropertyChanged(new PropertyChangedEventArgs("Progress")); }
        }

        public String ProgressText
        {
            get { return (FileInfo.Direction == FileSendDirection.Receive ? "Receiving " : "Sending ") + _progressText; }
            set { _progressText = value; }
        }

        public event EventHandler ProcessCancelFile;

        public void OnProcessCancelFile(EventArgs e)
        {
            EventHandler handler = ProcessCancelFile;
            if (handler != null) handler(this, e);
        }

        private ICommand _cancelFileSend;
        private int _progress;
        private string _progressText;

        public ICommand CancelFileSend
        {
            get { return _cancelFileSend ?? (_cancelFileSend = new CancelFileSendCommand(this.Manager, this)); }
        }

        internal class CancelFileSendCommand : ICommand
        {
            private readonly NotificationManager _notificationManager;
            private readonly FileSendProgressNotification _fileSendProgressNotification;

            public CancelFileSendCommand(NotificationManager notificationManager, FileSendProgressNotification fileSendProgressNotification)
            {
                _notificationManager = notificationManager;
                _fileSendProgressNotification = fileSendProgressNotification;
            }

            public void Execute(object parameter)
            {
                _notificationManager.DeleteNotification(_fileSendProgressNotification);
                _fileSendProgressNotification.OnProcessCancelFile(EventArgs.Empty);
            }

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged;
        }

        internal FileSendProgressNotification()
        {
            DeleteTimeout = 300;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, e);
        }
    }

}
