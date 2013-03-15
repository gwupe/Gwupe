using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlitsMe.Agent.Components.Functions.FileSend
{
    internal enum FileSendDirection { Send, Receive };
    class FileSendInfo
    {
        internal long FileSize;
        internal String Filename;
        internal String FileSendId;
        internal String FilePath;
        internal FileSendDirection Direction;
    }
}
