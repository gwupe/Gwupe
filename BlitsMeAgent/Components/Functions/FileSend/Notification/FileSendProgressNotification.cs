using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Input;
using Gwupe.Agent.Components.Notification;
using Gwupe.Agent.Managers;
using log4net;

namespace Gwupe.Agent.Components.Functions.FileSend.Notification
{
    class FileSendProgressNotification : CancellableNotification
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

        private int _progress;
        private string _progressText;
        private FileSendInfo _fileInfo;
    }

}
