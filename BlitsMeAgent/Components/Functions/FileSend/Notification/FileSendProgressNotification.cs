using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Input;
using BlitsMe.Agent.Components.Notification;
using BlitsMe.Agent.Managers;
using log4net;

namespace BlitsMe.Agent.Components.Functions.FileSend.Notification
{
    class FileSendProgressNotification : Components.Notification.Notification
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(FileSendProgressNotification));
        public FileSendInfo FileInfo
        {
            get { return _fileInfo; }
            set
            {
                _fileInfo = value;
                Message = (_fileInfo.Direction == FileSendDirection.Receive ? "Receiving " : "Sending ") +
                          _fileInfo.Filename;
            }
        }

        public int Progress
        {
            get { return _progress; }
            set { _progress = value; OnPropertyChanged("Progress"); }
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
        private FileSendInfo _fileInfo;

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
                Thread cancelThread = new Thread(() => _fileSendProgressNotification.OnProcessCancelFile(EventArgs.Empty)) { IsBackground = true };
                cancelThread.Start();
            }

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged;
        }

    }

}
