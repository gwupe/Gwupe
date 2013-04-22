using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Input;
using BlitsMe.Agent.Managers;
using log4net;

namespace BlitsMe.Agent.Components.Functions.FileSend.Notification
{
    class FileReceivedNotification : Components.Notification.Notification
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(FileReceivedNotification));
        internal FileSendInfo FileInfo
        {
            get { return _fileInfo; }
            set
            {
                _fileInfo = value;
                Message = "Received " + _fileInfo.Filename;
            }
        }

        private ICommand _openCommand;
        private FileSendInfo _fileInfo;

        public ICommand OpenCommand
        {
            get { return _openCommand ?? (_openCommand = new OpenFileReceivedCommand(this.Manager, this)); }
        }


        internal class OpenFileReceivedCommand : ICommand
        {
            private readonly NotificationManager _notificationManager;
            private readonly FileReceivedNotification _fileReceivedNotification;

            public OpenFileReceivedCommand(NotificationManager notificationManager, FileReceivedNotification fileReceivedNotification)
            {
                _notificationManager = notificationManager;
                this._fileReceivedNotification = fileReceivedNotification;
            }

            public void Execute(object parameter)
            {
                String type = (String)parameter;
                if (String.IsNullOrWhiteSpace(type))
                {
                    Logger.Error("Cannot execute openfile command, incorrect parameter");
                    return;
                }
                try
                {
                    if ("File".Equals(type))
                    {
                        Process.Start(_fileReceivedNotification.FileInfo.FilePath);
                    }
                    else if ("Folder".Equals(type))
                    {
                        Process.Start("explorer.exe", "/select, " + _fileReceivedNotification.FileInfo.FilePath);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to open the " + type + " associated with " + _fileReceivedNotification.FileInfo.FilePath, e);
                    return;
                }
                _notificationManager.DeleteNotification(_fileReceivedNotification);
            }

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged;

            public void OnCanExecuteChanged(EventArgs e)
            {
                EventHandler handler = CanExecuteChanged;
                if (handler != null) handler(this, e);
            }
        }
    }
}
