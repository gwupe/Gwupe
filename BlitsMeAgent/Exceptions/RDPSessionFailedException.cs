using System;

namespace BlitsMe.Agent.Exceptions
{
    internal class RDPSessionFailedException : Exception
    {
        public RDPSessionFailedException(string message)
            : base(message)
        {
        }
    }
}