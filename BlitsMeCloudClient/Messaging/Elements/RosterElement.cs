using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using BlitsMe.Cloud.Messaging.API;

namespace BlitsMe.Cloud.Messaging.Elements
{
    [DataContract]
    public class RosterElement
    {
        [DataMember] public UserElement userElement;
        [DataMember] public String shortCode;
        [DataMember] public IList<String> groups;
        [DataMember] public PresenceElement presence;
    }
}
