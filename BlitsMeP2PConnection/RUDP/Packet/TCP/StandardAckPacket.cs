using System;

namespace BlitsMe.Communication.P2P.RUDP.Packet.TCP
{
    public class StandardAckPacket : BasicTcpPacket
    {
        private const byte PKT_POS_CURRENT_DATA_ACK = 0;

        public StandardAckPacket()
        {
            Type = PKT_TYPE_ACK;
        }
        public StandardAckPacket(ushort ackSequence)
        {
            Sequence = ackSequence;
            Type = PKT_TYPE_ACK;
        }

        public override void ProcessPacket(byte[] sequencedBytes)
        {
            base.ProcessPacket(sequencedBytes);
            if (Payload != null)
            {
                CurrentDataAck = BitConverter.ToUInt16(Payload, PKT_POS_CURRENT_DATA_ACK);
            }
        }

        public ushort CurrentDataAck { get; set; }

        public override byte[] GetBytes()
        {
            Payload = new byte[2];
            Array.Copy(BitConverter.GetBytes(CurrentDataAck), 0, Payload, PKT_POS_CURRENT_DATA_ACK, 2);
            return base.GetBytes();
        }

        public override string ToString()
        {
            return base.ToString() + " ACK (dataAck=" + CurrentDataAck + ")";
        }
    }
}
