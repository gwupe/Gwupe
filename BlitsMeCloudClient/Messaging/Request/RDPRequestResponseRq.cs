using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace BlitsMe.Cloud.Messaging.Request
{
    public class RDPRequestResponseRq : API.Request
    {
        public override string type
        {
            get { return "RDPRequestResponse-RQ"; }
            set { }
        }

        [DataMember]
        public bool accepted { get; set; }
        [DataMember]
        public string shortCode { get; set; }
        [DataMember]
        public string username { get; set; }
    }
}
