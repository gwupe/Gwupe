using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace BlitsMe.Communication.P2P.RUDP.Utils
{
    public class PeerInfo
    {
        public IPEndPoint internalEndPoint;
        public IPEndPoint externalEndPoint;

        public PeerInfo(IPEndPoint internalEP, IPEndPoint externalEP)
        {
            internalEndPoint = internalEP;
            externalEndPoint = externalEP;
        }

        public PeerInfo()
        {
        }

        public override String ToString()
        {
            return "Internal => " + internalEndPoint + ", External => " + externalEndPoint;
        }
    }
}
