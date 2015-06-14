using System;
using System.Runtime.Serialization;
using Gwupe.Cloud.Messaging.Elements;

namespace Gwupe.Cloud.Messaging.Request
{
    [DataContract]
    public class PresenceChangeRq : API.Request
    {
        public override String type
        {
            get { return "PresenceChange-RQ"; }
            set { }
        }
        [DataMember]
        public String user { get; set; }

        [DataMember]
        public PresenceElement presence { get; set; }

        [DataMember]
        public String shortCode { get; set; }

        [DataMember]
        public String resource { get; set; }

        [DataMember]
        public ClientInfoElement clientInfo { get; set; }
    }
}
