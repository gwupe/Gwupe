using System;
using System.Runtime.Serialization;

namespace BlitsMe.Cloud.Messaging.Request
{
    [DataContract]
    public class InitP2PConnectionRq : BlitsMe.Cloud.Messaging.API.Request
    {
        public override String type
        {
            get { return "InitP2PConnection-RQ"; }
            set { }
        }
        [DataMember]
        public String sessionId { get; set; }
        [DataMember]
        public String shortCode { get; set; }
    }
}
