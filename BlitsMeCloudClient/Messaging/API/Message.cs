using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace BlitsMe.Cloud.Messaging.API
{
    [DataContract]
    public abstract class Message
    {
        [DataMember]
        public String id { get; set; }
        [DataMember]
        public DateTime date { get; set; }
        [DataMember]
        public abstract String type { get; set; }
    }
}
