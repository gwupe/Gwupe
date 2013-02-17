using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlitsMe.Agent.Components.Notification
{
    internal class FileSendEventArgs : EventArgs
    {
        internal String Filename { get; set; }
        internal String FileSendId { get; set; }

    }
}
