using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Gwupe.Cloud.Messaging.API;
using Gwupe.Cloud.Messaging.Elements;

namespace Gwupe.Cloud.Messaging.Response
{
    [DataContract]
    public class RosterRs : API.Response
    {
        public override String type
        {
            get { return "Roster-RS"; }
            set { }
        }

        [DataMember]
        public IList<RosterElement> rosterElements;
    }
}
