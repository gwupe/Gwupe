namespace BlitsMe.Communication.P2P.RUDP.Packet.Tunnel
{
    public class StandardTunnelDataPacket : BasicTunnelPacket
    {
        public StandardTunnelDataPacket()
        {
            this.type = PKT_TYPE_DATA;
        }

        public override string ToString()
        {
            return "DATA (payloadLength=" + data.Length + ")";
        }
    }
}
