using System;
using System.Collections.Generic;
using System.Deployment.Internal;
using System.Linq;
using System.Text;

namespace Gwupe.Agent.Components.Functions.FileSend
{
    internal enum FileSendDirection { Send, Receive };
    internal enum FileSendState { Initialised, PendingSend, Sending, SendCancelled, PendingReceive, Receiving, ReceiveCancelled, SendComplete, ReceiveComplete }
    class FileSendInfo
    {
        internal long FileSize;
        internal String Filename;
        internal String FileSendId;
        internal String FilePath;
        internal FileSendDirection Direction;
        internal FileSendState State = FileSendState.Initialised;
        internal Components.Notification.Notification Notification;
        internal FileSendListener FileReceiver;
        internal FileSendClient FileSender;

    }
}
