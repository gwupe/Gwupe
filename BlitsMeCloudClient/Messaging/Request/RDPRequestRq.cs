using System;
using System.Runtime.Serialization;

namespace Gwupe.Cloud.Messaging.Request
{
    [DataContract]
    public class RDPRequestRq : API.UserToUserRequest, API.IElevatedRequest
    {
        public override string type
        {
            get { return "RDPRequest-RQ"; }
            set { }
        }

        [DataMember]
        public String tokenId { get; set; }
        [DataMember]
        public String securityKey { get; set; }
    }
}
