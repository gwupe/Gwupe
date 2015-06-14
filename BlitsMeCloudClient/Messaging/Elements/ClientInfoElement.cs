using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Gwupe.Cloud.Messaging.Elements
{
    [DataContract]
    public class ClientInfoElement
    {
        [DataMember]
        public String version;

        [DataMember] 
        public String platform;
    }
}
