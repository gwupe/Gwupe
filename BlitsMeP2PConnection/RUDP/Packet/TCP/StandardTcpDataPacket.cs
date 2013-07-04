using System;
using System.Text;
using BlitsMe.Communication.P2P.RUDP.Packet.API;

namespace BlitsMe.Communication.P2P.RUDP.Packet.TCP
{
    public class StandardTcpDataPacket : BasicTcpPacket, ITcpDataPacket
    {
        private const byte PKT_POS_DATA = 2;
        private const byte PKT_POS_ACK = 0;
        public ushort Ack { get; set; }

        public byte[] Data
        {
            get { return _realData ?? new byte[0]; }
            set { _realData = value; }
        }

        private byte[] _realData;

        public StandardTcpDataPacket()
        {
            this.Type = PKT_TYPE_DATA;
        }
        public StandardTcpDataPacket(ushort sequenceOut)
        {
            this.Sequence = sequenceOut;
            this.Type = PKT_TYPE_DATA;
        }

        public override void ProcessPacket(byte[] sequencedBytes)
        {
            base.ProcessPacket(sequencedBytes);
            if (Payload != null)
            {
                Ack = BitConverter.ToUInt16(Payload, PKT_POS_ACK);
                Data = new byte[Payload.Length - PKT_POS_DATA];
                Array.Copy(Payload, PKT_POS_DATA, Data, 0, Data.Length);
            }
        }

        public override byte[] GetBytes()
        {
            Payload = new byte[2 + Data.Length];
            Array.Copy(BitConverter.GetBytes(Ack), 0, Payload, PKT_POS_ACK, 2);
            Array.Copy(Data, 0, Payload, PKT_POS_DATA, Data.Length);
            return base.GetBytes();
        }

        public override string ToString()
        {
            return base.ToString() + " DATA (dataLength=" + Data.Length + ",currentAck=" + Ack + ")";
        }
    }
}
