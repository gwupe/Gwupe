using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace BlitsMe.Cloud.Messaging.Response
{
    public class RDPRequestRs : API.Response
    {
        public override string type
        {
            get { return "RDPRequest-RS"; }
            set { }
        }

        [DataMember]
        public String shortCode { get; set; }
    }
}
