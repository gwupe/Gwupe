using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace BlitsMe.Cloud.Messaging.Response
{
    [DataContract]
    public class InitP2PConnectionRs : BlitsMe.Cloud.Messaging.API.Response
    {
        public override String type
        {
            get { return "InitP2PConnection-RS"; }
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
        [DataMember]
        public String username { get; set; }
        [DataMember]
        public String shortCode { get; set; }
        public override bool isValid()
        {
            return base.isValid() &&
                this.externalEndpointIp != null && !this.externalEndpointPort.Equals("0.0.0.0") &&
                this.externalEndpointPort != 0 &&
                this.username != null && this.username != "" &&
                this.shortCode != null && this.shortCode != "";
        }
    }
}
