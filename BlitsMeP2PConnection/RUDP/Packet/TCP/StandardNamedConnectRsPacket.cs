namespace BlitsMe.Communication.P2P.RUDP.Packet.TCP
{
    public class StandardNamedConnectRsPacket : BasicTcpPacket
    {
        public bool Success { get; set; }
        public byte RemoteConnectionId { get; set; }
        public byte ProtocolId { get; set; }

        public StandardNamedConnectRsPacket()
        {
            this.Type = PKT_TYPE_CONNECT_NAME_RS;
        }

        public override byte[] GetBytes()
        {
            Payload = new byte[3];
            Payload[0] = Success ? (byte)1 : (byte)0;
            Payload[1] = RemoteConnectionId;
            Payload[2] = ProtocolId;
            return base.GetBytes();
        }

        public override void ProcessPacket(byte[] sequencedBytes)
        {
            base.ProcessPacket(sequencedBytes);
            if (Payload != null && Payload.Length == 3)
            {
                Success = Payload[0] != 0;
                RemoteConnectionId = Payload[1];
                ProtocolId = Payload[2];
            }
        }

        public override string ToString()
        {
            return base.ToString() + " NAMED_CONNECT_RS (remoteConnectionId=" + RemoteConnectionId + ",protocolId=" + ProtocolId
                + ",result=" + (Success ? "succeeded" : "failed") + ")";
        }
    }
}