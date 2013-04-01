using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace BlitsMe.Cloud.Messaging.API
{
    [DataContract]
    public abstract class ElevatedRequest : Request
    {
        public override abstract String type { get; set; }
        [DataMember]
        public String tokenId;
        [DataMember]
        public String securityKey;
    }
}
