using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using BlitsMe.Cloud.Messaging.Elements;

namespace BlitsMe.Cloud.Messaging.Request
{
    [DataContract]
    public class PresenceChangeRq : BlitsMe.Cloud.Messaging.API.Request
    {
        public override String type
        {
            get { return "{PresenceChange-RQ"; }
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
    }
}
