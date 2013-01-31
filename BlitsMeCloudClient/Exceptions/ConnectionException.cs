using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlitsMe.Cloud.Exceptions
{
    class ConnectionException : Exception
    {
        public ConnectionException(String message) : base(message)
        {
        }

    }
}
