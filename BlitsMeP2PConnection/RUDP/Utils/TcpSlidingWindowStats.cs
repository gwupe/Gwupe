using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlitsMe.Communication.P2P.RUDP.Utils
{
    class TcpSlidingWindowStats
    {
        public int AcksReceived { get; set; }

        public long DataPacketsReceived { get; set; }

        public long OldDataPacketsReceived { get; set; }

        public long FutureDataPacketsReceived { get; set; }

        public long ExpectedDataPacketsReceived { get; set; }

        public float ExpectedOverReceivedPercentage
        {
            get
            {
                return DataPacketsReceived == 0 ? 0 : (float) ExpectedDataPacketsReceived / DataPacketsReceived;
            }
        }

        public float OldOverReceivedPercentage
        {
            get
            {
                return DataPacketsReceived == 0 ? 0 : (float)OldDataPacketsReceived / DataPacketsReceived;
            }
        }

        public float FutureOverReceivedPercentage
        {
            get
            {
                return DataPacketsReceived == 0 ? 0 : (float)FutureDataPacketsReceived / DataPacketsReceived;
            }
        }

        public long AcksSent { get; set; }

        public long AcksResent { get; set; }

        public long NoSpaceInWindow { get; set; }

        public long DataPacketBufferCount { get; set; }

        public long DataPacketSendCount { get; set; }

        public float AvgSendWaitTime { get; set; }

        public long DataPacketResendCount { get; set; }

        public long DataPacketFastRetransmitCount { get; set; }

        public long OldDataResendPacketsReceived { get; set; }

        public long OldAcksReceived { get; set; }

        public long HoldingAcksSent { get; set; }

        public String GetSenderStats()
        {
            return
                   "DataPacketBufferCount=" + DataPacketBufferCount + "\n"
                   + "NoSpaceInWindow=" + NoSpaceInWindow + "\n"
                   + "DataPacketSendCount=" + DataPacketSendCount + "\n"
                   + "DataPacketResendCount=" + DataPacketResendCount + "\n"
                   + "DataPacketFastRetransmitCount=" + DataPacketFastRetransmitCount + "\n"
                   + "AvgSendWaitTime=" + AvgSendWaitTime + "\n"
                   + "AcksReceived=" + AcksReceived + "\n"
                   + "OldAcksReceived=" + OldAcksReceived + "\n"
                ;
        }

        public String GetReceiverStats()
        {
            return
                   "DataPacketsReceived=" + DataPacketsReceived + "\n"
                   + "ExpectedDataPacketsReceived=" + ExpectedDataPacketsReceived + "\n"
                   + "ExpectedOverReceivedPercentage=" + ExpectedOverReceivedPercentage + "\n"
                   + "OldDataPacketsReceived=" + OldDataPacketsReceived + "\n"
                   + "OldOverReceivedPercentage=" + OldOverReceivedPercentage + "\n"
                   + "OldDataResendPacketsReceived=" + OldDataResendPacketsReceived + "\n"
                   + "FutureDataPacketsReceived=" + FutureDataPacketsReceived + "\n"
                   + "FutureOverReceivedPercentage=" + FutureOverReceivedPercentage + "\n"
                   + "AcksSent=" + AcksSent + "\n"
                   + "AcksResent=" + AcksResent + "\n"
                   + "HoldingAcksSent=" + HoldingAcksSent + "\n"

                ;
        }
    }
}
