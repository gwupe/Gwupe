using System;
using System.Runtime.Serialization;

namespace Gwupe.Cloud.Messaging.Request
{
    [DataContract]
    public class InitUDPConnectionRq : API.Request
    {
        public override String type
        {
            get { return "InitUDPConnection-RQ"; }
            set { }
        }
        [DataMember]
        public String facilitatorIP { get; set; }
        [DataMember]
        public int facilitatorPort { get; set; }
        [DataMember]
        public String uniqueId { get; set; }
        [DataMember]
        public String side { get; set; }
        [DataMember]
        public String encryptionKey { get; set; }
        [DataMember]
        public String shortCode { get; set; }
        [DataMember]
        public String username { get; set; }
    
        public bool IsClient { get { return side.Equals("CLIENT"); } }
        public bool IsServer { get { return side.Equals("SERVER"); } }
    }
}
