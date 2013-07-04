namespace BlitsMe.Communication.P2P.RUDP.Packet.TCP
{
    public class StandardDisconnectRsPacket : BasicTcpPacket
    {
        public StandardDisconnectRsPacket()
        {
            this.Type = PKT_TYPE_DISCONNECT_RS;
        }
        public StandardDisconnectRsPacket(byte connectionId)
        {
            this.ConnectionId = connectionId;
            this.Type = PKT_TYPE_DISCONNECT_RS;
        }

        public override string ToString()
        {
            return base.ToString() + " DISCONNECT_RS";
        }
    }
}
