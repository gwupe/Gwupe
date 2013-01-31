using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlitsMe.Communication.P2P.Exceptions
{
    public class P2PConnectionException : Exception
    {
        public P2PConnectionException(String message)
            : base(message)
        {
        }

        public P2PConnectionException(String message, Exception e)
            : base(message,e)
        {
        }    
    }
}
