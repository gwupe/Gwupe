namespace BlitsMe.Communication.P2P.RUDP.Packet.Tunnel
{
    public class StandardCloseTunnelPacket : BasicTunnelPacket
    {
        public StandardCloseTunnelPacket()
        {
            this.type = PKT_TYPE_CLOSE;
        }
    }
}
