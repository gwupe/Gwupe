using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace BlitsMe.Communication.P2P.RUDP.Packet.API
{
    public interface ITcpPacket : IPacket
    {
        ushort Sequence { get; }
        byte Type { get; }
        byte ResendCount { get; set; }
        byte ConnectionId { get; }
        // won't be part of the bytes, just useful to timestamp packets for various reasons
        long Timestamp { get; set; }
    }
}
