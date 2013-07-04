using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlitsMe.Agent.Components.Functions.FileSend
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
    }
}
