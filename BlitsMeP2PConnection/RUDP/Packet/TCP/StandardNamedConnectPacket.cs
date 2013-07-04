using System;
using System.Text;

namespace BlitsMe.Communication.P2P.RUDP.Packet.TCP
{
    public class StandardNamedConnectRqPacket : BasicTcpPacket
    {
        private const byte PKT_POS_PROTOCOL_ID = 0;
        private const byte PKT_POS_CONNECTION_NAME = 1;

        public String ConnectionName;
        public byte ProtocolId;

        public StandardNamedConnectRqPacket()
        {
            this.Type = PKT_TYPE_CONNECT_NAME_RQ;
        }

        public StandardNamedConnectRqPacket(byte connectionId)
        {
            this.ConnectionId = connectionId;
            this.Type = PKT_TYPE_CONNECT_NAME_RQ;
        }

        public override void ProcessPacket(byte[] sequencedBytes)
        {
            base.ProcessPacket(sequencedBytes);
            if (Payload != null)
            {
                ProtocolId = Payload[PKT_POS_PROTOCOL_ID];
                ConnectionName = Encoding.UTF8.GetString(Payload, PKT_POS_CONNECTION_NAME, Payload.Length - 1);
            }
        }

        public override byte[] GetBytes()
        {
            byte[] connectionNameData = Encoding.UTF8.GetBytes(ConnectionName);
            Payload = new byte[connectionNameData.Length + 1];
            Payload[PKT_POS_PROTOCOL_ID] = ProtocolId;
            Array.Copy(connectionNameData, 0, Payload, PKT_POS_CONNECTION_NAME, connectionNameData.Length);
            return base.GetBytes();
        }

        public override string ToString()
        {
            return base.ToString() + " NAMED_CONNECT_RQ (connectionName=" + ConnectionName + ",protocolId=" + ProtocolId + ")";
        }

    }
}
