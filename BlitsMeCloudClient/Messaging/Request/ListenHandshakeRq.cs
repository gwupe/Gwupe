using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace BlitsMe.Cloud.Messaging.Request
{
    [DataContract]
    public class ListenHandshakeRq : BlitsMe.Cloud.Messaging.API.UserToUserRequest
    {
        public override String type
        {
            get { return "ListenHandshake-RQ"; }
            set { }
        }
        [DataMember]
        public String externalEndpointIp { get; set; }
        [DataMember]
        public int externalEndpointPort { get; set; }
        [DataMember]
        public String internalEndpointIp { get; set; }
        [DataMember]
        public int internalEndpointPort { get; set; }
        [DataMember]
        public String uniqueId { get; set; }
    }
}
