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
        public String name;
        [DataMember]
        public String status;
        [DataMember]
        public String user;
        [DataMember]
        public String type;
        [DataMember]
        public String description;
        [DataMember]
        public String email;
        [DataMember]
        public String location;
        [DataMember]
        public int rating;
        [DataMember]
        public DateTime joined;

    }
}
