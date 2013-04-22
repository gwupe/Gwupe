using System;
using System.Windows.Input;
using BlitsMe.Agent.Components.Notification;
using BlitsMe.Agent.Managers;

namespace BlitsMe.Agent.Components.Functions.FileSend.Notification
{
    internal delegate void FileSendEventHandler(object sender, FileSendEventArgs args);

    class FileSendRequestNotification : TrueFalseNotification
    {
        internal FileSendInfo FileInfo;
    }

}
