namespace BlitsMe.Communication.P2P.RUDP.Packet.TCP
{
    public class StandardTcpNopPacket : BasicTcpPacket
    {
        public StandardTcpNopPacket() {
            this.Type = PKT_TYPE_NOP;
        }
    }
}
