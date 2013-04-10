using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace BlitsMe.Cloud.Messaging.Elements
{
    [DataContract]
    public class PresenceElement
    {
        [DataMember] public String type;
        [DataMember] public String mode;
        [DataMember] public String status;
        [DataMember] public int priority;
    }
}
