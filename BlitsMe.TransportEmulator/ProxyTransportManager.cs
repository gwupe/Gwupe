using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using BlitsMe.Communication.P2P.RUDP.Packet.API;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;
using BlitsMe.Communication.P2P.RUDP.Tunnel.Transport;

namespace BlitsMe.TransportEmulator
{
    class ProxyTransportManager : ITransportManager
    {
        public ITCPTransport TCPTransport { get; private set; }
        public UDPTransport UDPTransport { get; private set; }
        public ProxyTransportManager proxy;

        public ProxyTransportManager()
        {
            TCPTransport = new TCPTransport(this);
        }

        public void SendData(IPacket packet)
        {
            proxy.TCPTransport.ProcessPacket(packet.GetBytes());
     
        }

        public IPAddress RemoteIp { get { return IPAddress.Loopback; } }

        public void AddTunnel(IUDPTunnel tunnel, int priority)
        {
        }

        public void Close()
        {
        }

        public void SetProxy(ProxyTransportManager transportManager)
        {
            proxy = transportManager;
        }
    }
}
