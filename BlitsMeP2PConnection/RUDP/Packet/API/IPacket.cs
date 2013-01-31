using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlitsMe.Communication.P2P.RUDP.Packet.API
{
    public interface IPacket
    {
        byte[] Data { get; set; }

        // Convert bytes to the object
        void ProcessPacket(byte[] bytes);
        // convert the object to bytes
        byte[] GetBytes();
    }
}
