using BlitsMe.Communication.P2P.Exceptions;
using BlitsMe.Communication.P2P.RUDP.Packet.TCP;

namespace BlitsMe.Communication.P2P.RUDP.Packet
{
    public class StandardTcpPacketFactory
    {
        private static StandardTcpPacketFactory oneAndOnly;
        private StandardTcpPacketFactory()
        {
        }

        public static StandardTcpPacketFactory instance 
        {
            get
            {
                if (oneAndOnly == null)
                {
                    oneAndOnly = new StandardTcpPacketFactory();
                }
                return oneAndOnly;
            }
        }

        public BasicTcpPacket getPacket(byte[] bytes)
        {
            BasicTcpPacket packet;
            switch (bytes[BasicTcpPacket.PKT_POS_TYPE])
            {
                case BasicTcpPacket.PKT_TYPE_DISCONNECT:
                    packet = new StandardDisconnectPacket();
                    break;
                case BasicTcpPacket.PKT_TYPE_DISCONNECT_ACK:
                    packet = new StandardDisconnectAckPacket();
                    break;
                case BasicTcpPacket.PKT_TYPE_DISCONNECT_RS:
                    packet = new StandardDisconnectRsPacket();
                    break;
                case BasicTcpPacket.PKT_TYPE_CONNECT_RS_ACK:
                    packet = new StandardConnectRsAckPacket();
                    break;
                case BasicTcpPacket.PKT_TYPE_CONNECT_NAME_RS:
                    packet = new StandardNamedConnectRsPacket();
                    break;
                case BasicTcpPacket.PKT_TYPE_CONNECT_NAME_RQ:
                    packet = new StandardNamedConnectRqPacket();
                    break;
                case BasicTcpPacket.PKT_TYPE_DATA:
                    packet = new StandardTcpDataPacket();
                    break;
                case BasicTcpPacket.PKT_TYPE_ACK:
                    packet = new StandardAckPacket();
                    break;
                case BasicTcpPacket.PKT_TYPE_NOP:
                    packet = new StandardTcpNopPacket();
                    break;
                default:
                    throw new UnknownPacketException("Failed to determine packet type");
            }
            packet.ProcessPacket(bytes);
            return packet;
        }
    }
}
