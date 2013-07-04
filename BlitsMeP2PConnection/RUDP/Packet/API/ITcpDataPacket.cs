using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlitsMe.Communication.P2P.RUDP.Packet.API
{
    public interface ITcpDataPacket : ITcpPacket
    {
        // The actual data for the packet
        byte[] Data { get; }
        // The last packet acked on the reverse stream (piggy backing)
        ushort Ack { get; }
    }
}
