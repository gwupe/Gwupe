using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlitsMe.Communication.P2P.RUDP.Tunnel.API
{
    public interface ITransport
    {
        ITransportManager TransportManager { get; }
        void Close();
    }
}
