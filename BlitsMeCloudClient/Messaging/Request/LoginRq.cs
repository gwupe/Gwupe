using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace BlitsMe.Cloud.Messaging.Request
{
    [DataContract]
    class LoginRq : BlitsMe.Cloud.Messaging.API.Request
    {
        public override String type
        {
            get { return "Login-RQ"; }
            set { }
        }
        [DataMember]
        public string username { get; set; }
        [DataMember]
        public string passwordDigest { get; set; }
        [DataMember]
        public string profile { get; set; }
        [DataMember]
        public string workstation { get; set; }
    }
}
