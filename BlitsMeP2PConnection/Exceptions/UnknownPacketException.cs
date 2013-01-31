using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlitsMe.Communication.P2P.Exceptions
{
    public class UnknownPacketException : Exception
    {
        public UnknownPacketException(String message)
            : base(message)
        {
        }

        public UnknownPacketException(String message, Exception e)
            : base(message, e)
        {
        }
    }
}
