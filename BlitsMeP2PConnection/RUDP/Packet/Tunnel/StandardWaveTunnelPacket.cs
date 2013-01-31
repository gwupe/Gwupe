using System;
using System.Net;

namespace BlitsMe.Communication.P2P.RUDP.Packet.Tunnel
{
    internal abstract class StandardWaveTunnelPacket : BasicTunnelPacket
    {
        private const byte POS_IADDR = 0;
        private const byte POS_IPORT = 4;
        private const byte POS_EADDR = 8;
        private const byte POS_EPORT = 12;

        internal IPEndPoint internalEndPoint = new IPEndPoint(IPAddress.Any, 0);
        internal IPEndPoint externalEndPoint = new IPEndPoint(IPAddress.Any, 0);

        public override void processPacket(byte[] bytes, IPEndPoint ip)
        {
            base.processPacket(bytes, ip);
            byte[] tempAddrBytes = new byte[4];
            byte[] tempPortBytes = new byte[4];
            Array.Copy(data, POS_IADDR, tempAddrBytes, 0, 4);
            Array.Copy(data, POS_IPORT, tempPortBytes, 0, 4);
            internalEndPoint = new IPEndPoint(new IPAddress(tempAddrBytes),BitConverter.ToInt32(tempPortBytes, 0));
            Array.Copy(data, POS_EADDR, tempAddrBytes, 0, 4);
            Array.Copy(data, POS_EPORT, tempPortBytes, 0, 4);
            externalEndPoint = new IPEndPoint(new IPAddress(tempAddrBytes), BitConverter.ToInt32(tempPortBytes, 0));
        }

        public override byte[] getBytes()
        {
            data = new byte[16];
            // internalIP
            Array.Copy(internalEndPoint.Address.GetAddressBytes(), 0, data, POS_IADDR, 4);
            // internalPort
            Array.Copy(P2PUtils.instance.getPortBytes(internalEndPoint.Port), 0, data, POS_IPORT, 4);
            // externalIP
            Array.Copy(externalEndPoint.Address.GetAddressBytes(), 0, data, POS_EADDR, 4);
            // externalPort
            Array.Copy(P2PUtils.instance.getPortBytes(externalEndPoint.Port), 0, data, POS_EPORT, 4);
            return base.getBytes();
        }
    }

    internal class StandardWaveTunnelRqPacket : StandardWaveTunnelPacket
    {
        internal StandardWaveTunnelRqPacket()
        {
            type = PKT_TYPE_WAVE_RQ;
        }
    }

    internal class StandardWaveTunnelRsPacket : StandardWaveTunnelPacket
    {
        internal StandardWaveTunnelRsPacket()
        {
            type = PKT_TYPE_WAVE_RS;
        }
    }
}
