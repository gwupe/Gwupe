using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlitsMe.Communication.P2P.Exceptions
{
    class BufferUnderrunException : Exception
    {
        public BufferUnderrunException(String message)
            : base(message)
        {
        }

        public BufferUnderrunException(String message, Exception e)
            : base(message, e)
        {
        }
    }
}
