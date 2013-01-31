namespace BlitsMe.Communication.P2P.RUDP.Packet.TCP
{
    public class StandardAckPacket : BasicTcpPacket
    {
        public StandardAckPacket()
        {
            Type = PKT_TYPE_ACK;
        }
        public StandardAckPacket(ushort ackSequence)
        {
            Sequence = ackSequence;
            Type = PKT_TYPE_ACK;
        }
        public override string ToString()
        {
            return "[" + ConnectionId + "/" + Sequence + "] ACK (payloadLength=" + Data.Length + ",sendCount=" + ResendCount + ")";
        }
    }
}
