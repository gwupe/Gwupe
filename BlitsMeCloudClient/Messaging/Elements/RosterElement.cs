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
        [DataMember] public String name;
        [DataMember] public String status;
        [DataMember] public String user;
        [DataMember] public String type;
        [DataMember] public String email;
        [DataMember] public String location;
        [DataMember] public String shortCode;
        [DataMember] public int rating;
        [DataMember] public IList<String> groups;
        [DataMember] public PresenceElement presence;
        [DataMember] public DateTime joined;
    }
}
