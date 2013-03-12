using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace BlitsMe.Cloud.Messaging.Request
{
    [DataContract]
    public class InitUDPConnectionRq : BlitsMe.Cloud.Messaging.API.Request
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
        public bool isClient { get { return side.Equals("CLIENT"); } }
        public bool isServer { get { return side.Equals("SERVER"); } }
    }
}
