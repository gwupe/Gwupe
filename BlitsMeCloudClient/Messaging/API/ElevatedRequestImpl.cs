using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Gwupe.Cloud.Messaging.API
{
    [DataContract]
    public abstract class ElevatedRequestImpl : Request, IElevatedRequest
    {
        public override abstract String type { get; set; }
        [DataMember]
        public String tokenId { get; set; }
        [DataMember]
        public String securityKey { get; set; }
    }
}
