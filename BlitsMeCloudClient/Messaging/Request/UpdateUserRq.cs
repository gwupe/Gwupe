using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using BlitsMe.Cloud.Messaging.Elements;

namespace BlitsMe.Cloud.Messaging.Request
{
    [DataContract]
    public class UpdateUserRq : API.ElevatedRequest
    {
        public override string type { get { return "UpdateUser-RQ"; } set { }
        }

        [DataMember] public String password;
        [DataMember]
        public UserElement userElement;
    }
}
