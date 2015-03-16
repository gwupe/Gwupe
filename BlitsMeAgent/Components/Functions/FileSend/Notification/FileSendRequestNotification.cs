using System;
using System.Windows.Input;
using Gwupe.Agent.Components.Notification;
using Gwupe.Agent.Managers;

namespace Gwupe.Agent.Components.Functions.FileSend.Notification
{
    internal delegate void FileSendEventHandler(object sender, FileSendEventArgs args);

    class FileSendRequestNotification : TrueFalseNotification
    {
        internal FileSendInfo FileInfo;
    }

}
