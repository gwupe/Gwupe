using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace BlitsMe.Communication.P2P.RUDP.Packet.API
{
    internal interface ITunnelPacket
    {
        byte[] data { get; set; }
        byte type { get; set; }
        IPEndPoint ip { get; set; }

        // Convert bytes to the object
        void processPacket(byte[] bytes, IPEndPoint ip);
        // convert the object to bytes
        byte[] getBytes();
    }
}
