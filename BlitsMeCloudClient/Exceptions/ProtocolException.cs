using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gwupe.Cloud.Exceptions
{
    public class ProtocolException : Exception
    {
        public ProtocolException(String message) : base(message) {}
    }
}
