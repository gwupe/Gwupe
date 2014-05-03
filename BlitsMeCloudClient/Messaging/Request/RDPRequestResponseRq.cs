using System;
using System.Runtime.Serialization;

namespace BlitsMe.Cloud.Messaging.Request
{
    public class RDPRequestResponseRq : API.UserToUserRequest
    {
        public override string type
        {
            get { return "RDPRequestResponse-RQ"; }
            set { }
        }

        [DataMember]
        public bool accepted { get; set; }
        [DataMember]
        public String connectionId { get; set; }
    }
}
