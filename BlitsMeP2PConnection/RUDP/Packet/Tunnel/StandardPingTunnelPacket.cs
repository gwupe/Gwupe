namespace BlitsMe.Communication.P2P.RUDP.Packet.Tunnel
{
    public class StandardPingTunnelPacket : BasicTunnelPacket
    {
        public StandardPingTunnelPacket()
        {
            this.type = PKT_TYPE_PING;
        }
    }

    public class StandardPongTunnelPacket : BasicTunnelPacket
    {
        public StandardPongTunnelPacket()
        {
            this.type = PKT_TYPE_PONG;
        }
    }
}
