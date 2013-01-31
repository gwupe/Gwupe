using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlitsMe.Communication.P2P.Exceptions
{
    class BufferOverrunException : Exception
    {
        public BufferOverrunException(String message)
            : base(message)
        {
        }

        public BufferOverrunException(String message, Exception e)
            : base(message, e)
        {
        }
    }
}
