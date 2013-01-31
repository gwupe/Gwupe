using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlitsMe.Communication.P2P.RUDP.Packet.API;

namespace BlitsMe.Communication.P2P.RUDP.Tunnel.API
{
    public interface ITransportConnection
    {
        void Open();
        void Close();
    }
}
