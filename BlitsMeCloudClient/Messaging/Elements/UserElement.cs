using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace BlitsMe.Cloud.Messaging.Elements
{
    [DataContract]
    public class UserElement
    {
        [DataMember]
        public String firstname;
        [DataMember]
        public String lastname;
        [DataMember]
        public String name;
        [DataMember]
        public String subscriptionStatus;
        [DataMember]
        public String user;
        [DataMember]
        public String subscriptionType;
        [DataMember]
        public String description;
        [DataMember]
        public String email;
        [DataMember]
        public String location;
        [DataMember]
        public int rating;
        [DataMember]
        public DateTime? joined;
        [DataMember]
        public String avatarData;
        [DataMember]
        public bool hasAvatar;
        [DataMember] 
        public bool supporter;
    }
}
