namespace BlitsMe.Communication.P2P.RUDP.Packet.TCP
{
    public class StandardConnectRsAckPacket : BasicTcpPacket
    {
        public StandardConnectRsAckPacket()
        {
            this.Type = PKT_TYPE_CONNECT_RS_ACK;
        }
        public StandardConnectRsAckPacket(byte connectionId)
        {
            this.ConnectionId = connectionId;
            this.Type = PKT_TYPE_CONNECT_RS_ACK;
        }

        public override string ToString()
        {
            return base.ToString() + " CONNECT_RS_ACK";
        }
    }
}
