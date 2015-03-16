using System;

namespace Gwupe.Agent.Exceptions
{
    internal class RDPSessionFailedException : Exception
    {
        public RDPSessionFailedException(string message)
            : base(message)
        {
        }
    }
}