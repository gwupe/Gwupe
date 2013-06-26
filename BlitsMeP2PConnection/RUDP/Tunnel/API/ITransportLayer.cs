using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlitsMe.Communication.P2P.RUDP.Packet.API;

namespace BlitsMe.Communication.P2P.RUDP.Tunnel.API
{
    public interface ITransportLayer
    {
        bool Closed { get; }
        bool Closing { get; }
        void Open();
        void Close();
    }
}
