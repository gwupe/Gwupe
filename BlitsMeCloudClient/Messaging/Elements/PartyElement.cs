using System;
using System.Runtime.Serialization;

namespace Gwupe.Cloud.Messaging.Elements
{
    [DataContract]
    public class PartyElement
    {
        [DataMember]
        public String firstname;
        [DataMember]
        public String lastname;
        [DataMember]
        public String name;
        [DataMember]
        public String user;
        [DataMember]
        public String uniqueHandle;
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
        [DataMember]
        public String subscriptionStatus;
        [DataMember]
        public String subscriptionType;

    }
}