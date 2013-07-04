using System;
using BlitsMe.Communication.P2P.RUDP.Packet.API;

namespace BlitsMe.Communication.P2P.RUDP.Packet.TCP
{
    public abstract class BasicTcpPacket : ITcpPacket, IComparable
    {
        // Positions
        private const byte PKT_POS_SEQ = 0;
        public const byte PKT_POS_TYPE = 2;
        private const byte PKT_POS_RESEND_COUNT = 3;
        private const byte PKT_POS_CONNECTION_ID = 4;
        private const byte PKT_POS_PAYLOAD = 5;

        // Types
        internal const byte PKT_TYPE_DATA = 0;
        internal const byte PKT_TYPE_ACK = 1;
        internal const byte PKT_TYPE_CONNECT_NAME_RQ = 8;
        internal const byte PKT_TYPE_CONNECT_NAME_RS = 9;
        internal const byte PKT_TYPE_CONNECT_PROXY_RQ = 10;
        internal const byte PKT_TYPE_CONNECT_PROXY_RS = 11;
        internal const byte PKT_TYPE_CONNECT_RS_ACK = 12;
        internal const byte PKT_TYPE_NOP = 32;
        internal const byte PKT_TYPE_DISCONNECT = 33;
        internal const byte PKT_TYPE_DISCONNECT_ACK = 34;
        internal const byte PKT_TYPE_DISCONNECT_RS = 35;

        // Internal data
        public ushort Sequence { get; set; }
        public byte Type { get; set; }
        public byte ResendCount { get; set; }
        public byte ConnectionId { get; set; }

        private byte[] _payload;
        public byte[] Payload
        {
            get { return _payload ?? new byte[0]; }
            set { _payload = value; }
        }

        public long Timestamp { get; set; }

        private const int SEQ_UPPER_BOUND = ushort.MaxValue + 1;

        public virtual void ProcessPacket(byte[] sequencedBytes)
        {
            Sequence = BitConverter.ToUInt16(sequencedBytes, PKT_POS_SEQ);
            Type = sequencedBytes[PKT_POS_TYPE];
            ResendCount = sequencedBytes[PKT_POS_RESEND_COUNT];
            ConnectionId = sequencedBytes[PKT_POS_CONNECTION_ID];
            Payload = new byte[sequencedBytes.Length - PKT_POS_PAYLOAD];
            Array.Copy(sequencedBytes, PKT_POS_PAYLOAD, Payload, 0, Payload.Length);
        }

        public virtual byte[] GetBytes()
        {
            byte[] bytes = new byte[Payload.Length + PKT_POS_PAYLOAD];
            // Sequence
            Array.Copy(BitConverter.GetBytes(Sequence), 0, bytes, PKT_POS_SEQ, 2);
            // Type
            bytes[PKT_POS_TYPE] = Type;
            // Resend Count
            bytes[PKT_POS_RESEND_COUNT] = ResendCount;
            // Connection id 
            bytes[PKT_POS_CONNECTION_ID] = ConnectionId;
            // Data
            Array.Copy(Payload, 0, bytes, PKT_POS_PAYLOAD, Payload.Length);
            return bytes;
        }

        public int CompareTo(Object obj)
        {
            ITcpPacket packet = (ITcpPacket)obj;
            return CompareSequences(Sequence, packet.Sequence);
        }

        public static int CompareSequences(int seq1, int seq2)
        {
            int result = 0;
            if ((SEQ_UPPER_BOUND - seq1 < 256) && (seq2 < 256))
            {
                // seq1 is within range of the max value and seq2 has rolled over
                // add seq2 to the max before comparison
                seq2 = SEQ_UPPER_BOUND + seq2;
            }
            else if ((SEQ_UPPER_BOUND - seq2 < 256) && (seq1 < 256))
            {
                // If the reverse is true, then the same in reverse applies
                seq1 = SEQ_UPPER_BOUND + seq1;
            }
            if (seq1 < seq2) { result = -1; }
            else if (seq1 > seq2) { result = 1; }
            return result;
        }

        public override string ToString()
        {
            return "[" + ConnectionId + "-" + Sequence + "-" + ResendCount + "-" + Payload.Length + "]";
        }
    }
}
