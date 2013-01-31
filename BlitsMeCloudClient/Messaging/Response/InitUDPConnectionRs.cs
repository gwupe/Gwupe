using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using BlitsMe.Communication.P2P;
using BlitsMe.Communication.P2P.RUDP.Utils;

namespace BlitsMe.Cloud.Messaging.Response
{
    [DataContract]
    public class InitUDPConnectionRs : BlitsMe.Cloud.Messaging.API.Response
    {
        public override String type
        {
            get { return "InitUDPConnection-RS"; }
            set { }
        }

        public void setUDPPeerInfo(PeerInfo info) {
            this.internalIP = info.internalEndPoint.Address.ToString();
            this.internalPort = info.internalEndPoint.Port;
            this.externalIP = info.externalEndPoint.Address.ToString();
            this.externalPort = info.externalEndPoint.Port;
        }

        [DataMember]
        public String internalIP { get; set; }
        [DataMember]
        public String externalIP { get; set; }
        [DataMember]
        public int internalPort { get; set; }
        [DataMember]
        public int externalPort { get; set; }

    }
}
