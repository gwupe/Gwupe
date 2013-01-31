namespace BlitsMe.Communication.P2P.RUDP.Packet.Tunnel
{
    public class StandardTunnelNopPacket : BasicTunnelPacket
    {
        public StandardTunnelNopPacket() {
            this.type = PKT_TYPE_NOP;
        }
    }
}
