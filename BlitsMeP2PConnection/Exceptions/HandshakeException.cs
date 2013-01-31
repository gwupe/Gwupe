using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlitsMe.Communication.P2P.Exceptions
{
    public class HandshakeException : Exception
    {
        public HandshakeException(String message)
            : base(message)
        {
        }

        public HandshakeException(String message, Exception e)
            : base(message,e)
        {
        }
    }
}
