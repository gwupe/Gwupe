using System;
using BlitsMe.Communication.P2P.RUDP.Packet.API;
using System.Net;

namespace BlitsMe.Communication.P2P.RUDP.Packet.Tunnel
{
    public abstract class BasicTunnelPacket : ITunnelPacket
    {
        // Positons
        public const byte PKT_POS_TYPE = 0;
        private const byte PKT_POS_DATA = 1;

        // Types
        internal const byte PKT_TYPE_WAVE_RQ = 0;
        internal const byte PKT_TYPE_WAVE_RS = 1;
        internal const byte PKT_TYPE_SYNC_RQ = 2;
        internal const byte PKT_TYPE_SYNC_RS = 3;
        internal const byte PKT_TYPE_NOP = 4;
        internal const byte PKT_TYPE_DATA = 5;
        internal const byte PKT_TYPE_PING = 6;
        internal const byte PKT_TYPE_PONG = 7;
        internal const byte PKT_TYPE_CLOSE = 8;

        // Internal data
        public byte type { get; set; }
        public byte[] data
        {
            get { return realData == null ? new byte[0] : realData; }
            set { realData = value; }
        }
        public IPEndPoint ip { get; set; }

        private byte[] realData;

        public virtual void processPacket(byte[] bytes, IPEndPoint ip)
        {
            type = bytes[PKT_POS_TYPE];
            data = new byte[bytes.Length - PKT_POS_DATA];
            Array.Copy(bytes, PKT_POS_DATA, data, 0, data.Length);
            this.ip = ip;
        }

        public virtual byte[] getBytes()
        {
            byte[] bytes = new byte[data.Length + PKT_POS_DATA];
            // Type
            bytes[PKT_POS_TYPE] = type;
            // Data
            Array.Copy(data, 0, bytes, PKT_POS_DATA, data.Length);
            return bytes;
        }

        public override string ToString()
        {
            return type + " (payloadLength=" + data.Length + ")";
        }
    }
}
