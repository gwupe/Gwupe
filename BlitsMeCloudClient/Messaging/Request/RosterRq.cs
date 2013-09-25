using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace BlitsMe.Cloud.Messaging.Request
{
    [DataContract]
    public class RosterRq : API.Request
    {
        public override String type
        {
            get { return "Roster-RQ"; }
            set { }
        }

    }
}
