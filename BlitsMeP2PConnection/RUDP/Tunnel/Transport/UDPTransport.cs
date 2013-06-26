using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;

namespace BlitsMe.Communication.P2P.RUDP.Tunnel.Transport
{
    public class UDPTransport : ITransport 
    {
        public ITransportManager TransportManager { get; private set; }

        public UDPTransport(ITransportManager transportManager)
        {
            TransportManager = transportManager;
        }

        public void Close()
        {
            throw new NotImplementedException();
        }
    }
}
