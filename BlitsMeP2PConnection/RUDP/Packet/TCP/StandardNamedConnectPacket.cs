using System;
using System.Text;

namespace BlitsMe.Communication.P2P.RUDP.Packet.TCP
{
    public class StandardNamedConnectRqPacket : BasicTcpPacket
    {
        public String connectionName;

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
            connectionName = Encoding.UTF8.GetString(Data);
        }

        public override byte[] GetBytes()
        {
            Data = Encoding.UTF8.GetBytes(connectionName);
            return base.GetBytes();
        }

        public override string ToString()
        {
            return base.ToString("NAMED_CONNECT_RQ (" + connectionName + ")");
        }

    }

    public class StandardNamedConnectRsPacket : BasicTcpPacket
    {
        public bool success { get; set; }
        public byte remoteConnectionId { get; set; }

        public StandardNamedConnectRsPacket()
        {
            this.Type = PKT_TYPE_CONNECT_NAME_RS;
        }

        public override byte[] GetBytes()
        {
            Data = new byte[2];
            Data[0] = success ? (byte)1 : (byte)0;
            Data[1] = remoteConnectionId;
            return base.GetBytes();
        }

        public override void ProcessPacket(byte[] sequencedBytes)
        {
            base.ProcessPacket(sequencedBytes);
            if (Data != null && Data.Length == 2)
            {
                success = Data[0] == 0 ? false : true;
                remoteConnectionId = Data[1];
            }
        }

        public override string ToString()
        {
            return base.ToString("[" + remoteConnectionId + "] NAMED_CONNECT_RS (" + (success ? "succeeded" : "failed") + ")");
        }
    }

}
