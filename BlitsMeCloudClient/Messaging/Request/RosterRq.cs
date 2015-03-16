using System;
using System.Runtime.Serialization;

namespace Gwupe.Cloud.Messaging.Request
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
