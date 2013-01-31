using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlitsMe.Cloud.Exceptions
{
    public class MessageException : Exception
    {
        // there is a message coming back which is details an error of some sort with the handling of the message
        public String ErrorCode;
        public MessageException(String message)
            : base(message)
        {
        }

        public MessageException(String message, String errorCode) : base("[" + errorCode + "] " + message)
        {
            this.ErrorCode = errorCode;
        }
    }
}
