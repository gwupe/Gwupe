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
        [DataMember(Order = 0)]
        public abstract String type { get; set; }
        [DataMember(Order = 1)]
        public String id { get; set; }
        [DataMember]
        public DateTime date { get; set; }
    }
}
