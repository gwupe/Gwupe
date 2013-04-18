using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlitsMe.Communication.P2P.RUDP.Packet.API;
using BlitsMe.Communication.P2P.RUDP.Packet.TCP;
using BlitsMe.Communication.P2P.RUDP.Socket.API;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;

namespace BlitsMe.Communication.P2P.RUDP.Tunnel
{
    class TcpTransportSlidingWindow : TcpTransportLayerOne4One 
    {
        private const int WindowSize = 10;
        private ITcpPacket[] _window;
        private ushort _current, _nr;
        private Object _pdpLock = new Object();

        public TcpTransportSlidingWindow(ITCPTransport transport, byte connectionId) : base(transport, connectionId)
        {
            _window = new ITcpPacket[WindowSize];
            _current = 0;
            _nr = 1;
        }

        public new void ProcessDataPacket(ITcpPacket packet)
        {
            lock(_pdpLock)
            {
                if (packet.Sequence == _nr)
                {
                    
                }
            }
        }

        public new void SendData(byte[] data, int timeout)
        {
        }

        public new void ProcessAck(StandardAckPacket packet)
        {
        }
    }
}
