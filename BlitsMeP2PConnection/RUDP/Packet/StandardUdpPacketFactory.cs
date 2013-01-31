using BlitsMe.Communication.P2P.Exceptions;
using System.Net;
using BlitsMe.Communication.P2P.RUDP.Packet.Tunnel;

namespace BlitsMe.Communication.P2P.RUDP.Packet
{
    public class StandardUdpPacketFactory
    {
        private static StandardUdpPacketFactory oneAndOnly;
        private StandardUdpPacketFactory()
        {
        }

        public static StandardUdpPacketFactory instance 
        {
            get
            {
                if (oneAndOnly == null)
                {
                    oneAndOnly = new StandardUdpPacketFactory();
                }
                return oneAndOnly;
            }
        }

        public BasicTunnelPacket getPacket(byte[] bytes, IPEndPoint ip)
        {
            BasicTunnelPacket packet;
            switch (bytes[BasicTunnelPacket.PKT_POS_TYPE])
            {
                case BasicTunnelPacket.PKT_TYPE_PING:
                    packet = new StandardPingTunnelPacket();
                    break;
                case BasicTunnelPacket.PKT_TYPE_PONG:
                    packet = new StandardPongTunnelPacket();
                    break;
                case BasicTunnelPacket.PKT_TYPE_SYNC_RS:
                    packet = new StandardSyncRsTunnelPacket();
                    break;
                case BasicTunnelPacket.PKT_TYPE_SYNC_RQ:
                    packet = new StandardSyncRqTunnelPacket();
                    break;
                case BasicTunnelPacket.PKT_TYPE_WAVE_RS:
                    packet = new StandardWaveTunnelRsPacket();
                    break;
                case BasicTunnelPacket.PKT_TYPE_WAVE_RQ:
                    packet = new StandardWaveTunnelRqPacket();
                    break;
                case BasicTunnelPacket.PKT_TYPE_DATA:
                    packet = new StandardTunnelDataPacket();
                    break;
                case BasicTunnelPacket.PKT_TYPE_NOP:
                    packet = new StandardTunnelNopPacket();
                    break;
                case BasicTunnelPacket.PKT_TYPE_CLOSE:
                    packet = new StandardCloseTunnelPacket();
                    break;
                default:
                    throw new UnknownPacketException("Failed to determine packet type [" + bytes[BasicTunnelPacket.PKT_POS_TYPE] + "]");
            }
            packet.processPacket(bytes, ip);
            return packet;
        }
    }
}
