using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlitsMe.Communication.P2P.Exceptions
{
    public class ConnectionException : Exception
    {
        public ConnectionException(String message)
            : base(message)
        {
        }

        public ConnectionException(String message, Exception e)
            : base(message, e)
        {
        }
    }
}
