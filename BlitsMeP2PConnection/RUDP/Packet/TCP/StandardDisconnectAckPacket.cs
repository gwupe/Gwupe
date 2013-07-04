namespace BlitsMe.Communication.P2P.RUDP.Packet.TCP
{
    internal class StandardDisconnectAckPacket : BasicTcpPacket
    {
        public StandardDisconnectAckPacket()
        {
            this.Type = PKT_TYPE_DISCONNECT_ACK;
        }
        public StandardDisconnectAckPacket(byte connectionId)
        {
            this.ConnectionId = connectionId;
            this.Type = PKT_TYPE_DISCONNECT_ACK;
        }

        public override string ToString()
        {
            return base.ToString() + " DISCONNECT_ACK";
        }
    }
}