namespace BlitsMe.Communication.P2P.RUDP.Packet.TCP
{
    public class StandardTcpDataPacket : BasicTcpPacket
    {
        public StandardTcpDataPacket()
        {
            this.Type = PKT_TYPE_DATA;
        }
        public StandardTcpDataPacket(ushort sequenceOut)
        {
            this.Sequence = sequenceOut;
            this.Type = PKT_TYPE_DATA;
        }

        public override string ToString()
        {
            return "[" + ConnectionId + "/" + Sequence + "] DATA (payloadLength=" + Data.Length + ",sendCount=" + ResendCount + ")";
        }
    }
}
