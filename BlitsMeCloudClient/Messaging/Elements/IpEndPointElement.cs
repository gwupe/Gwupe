using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;

namespace BlitsMe.Cloud.Messaging.Elements
{
    [DataContract]
    public class IpEndPointElement
    {
        [DataMember] public String address;
        [DataMember] public int port;

        public IpEndPointElement() {}

        public IpEndPointElement(IPEndPoint externalEndPoint)
        {
            address = externalEndPoint.Address.ToString();
            port = externalEndPoint.Port;
        }
    }
}
