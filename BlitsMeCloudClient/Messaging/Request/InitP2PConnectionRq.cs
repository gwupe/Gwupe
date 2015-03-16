using System;
using System.Runtime.Serialization;

namespace Gwupe.Cloud.Messaging.Request
{
    [DataContract]
    public class InitP2PConnectionRq : Gwupe.Cloud.Messaging.API.Request
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
        [DataMember]
        public String connectionId { get; set; }
    }
}
