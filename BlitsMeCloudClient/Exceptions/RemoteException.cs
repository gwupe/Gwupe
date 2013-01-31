using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlitsMe.Cloud.Exceptions
{
    // This gets thrown if there was an exception on the remote side causing the message to 
    // not be processed correctly (so an appropriate response cannot be sent back)
    public class RemoteException : Exception
    {
        public String ErrorCode;
        public RemoteException(String message, String errorCode) : base("[" + errorCode + "] " + message)
        {
            this.ErrorCode = errorCode;
        }
    }
}
