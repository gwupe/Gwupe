using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlitsMe.Agent.Components
{
    internal abstract class EngagementActivityArgs : EventArgs
    {
        public abstract String ActivityType { get; }
        public Engagement Engagement { get; set; }
        public String From { get; set; }
        public String To { get; set; }

        internal EngagementActivityArgs(Engagement engagement)
        {
            Engagement = engagement;
        }
    }


    internal class RDPSessionRequestResponseArgs : EngagementActivityArgs
    {

        public override string ActivityType
        {
            get { return "RDP_SESSION_REQUEST_RESPONSE"; }
        }

        internal RDPSessionRequestResponseArgs(Engagement engagement)
            : base(engagement)
        {

        }
    }

    internal class RDPIncomingRequestArgs : EngagementActivityArgs
    {
        public override string ActivityType
        {
            get { return "RDP_INCOMING_REQUEST"; }
        }

        internal RDPIncomingRequestArgs(Engagement engagement)
            : base(engagement)
        {

        }
    }
}
