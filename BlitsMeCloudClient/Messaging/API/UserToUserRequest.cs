using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace BlitsMe.Cloud.Messaging.API
{
    [DataContract]
    public abstract class UserToUserRequest : API.Request
    {
        public override abstract string type { get; set; }

        [DataMember]
        public String shortCode { get; set; }

        [DataMember]
        public String username { get; set; }
    }
}
