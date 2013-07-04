namespace BlitsMe.Communication.P2P.RUDP.Packet.TCP
{
    public class StandardDisconnectPacket : BasicTcpPacket
    {
        public StandardDisconnectPacket()
        {
            this.Type = PKT_TYPE_DISCONNECT;
        }

        public StandardDisconnectPacket(byte connectionId)
        {
            this.ConnectionId = connectionId;
            this.Type = PKT_TYPE_DISCONNECT;
        }
        public override string ToString()
        {
            return base.ToString() + " DISCONNECT";
        }
    }
}
