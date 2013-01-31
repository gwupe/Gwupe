namespace BlitsMe.Communication.P2P.RUDP.Packet.Tunnel
{
    public class StandardSyncRqTunnelPacket : BasicTunnelPacket
    {
        public StandardSyncRqTunnelPacket()
        {
            this.type = PKT_TYPE_SYNC_RQ;
        }
    }

    public class StandardSyncRsTunnelPacket : BasicTunnelPacket
    {
        public StandardSyncRsTunnelPacket()
        {
            this.type = PKT_TYPE_SYNC_RS;
        }
    }
}
