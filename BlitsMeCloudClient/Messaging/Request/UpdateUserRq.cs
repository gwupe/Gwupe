using System;
using System.Runtime.Serialization;
using Gwupe.Cloud.Messaging.Elements;

namespace Gwupe.Cloud.Messaging.Request
{
    [DataContract]
    public class UpdateUserRq : API.ElevatedRequestImpl
    {
        public override string type { get { return "UpdateUser-RQ"; } set { }
        }

        [DataMember] public String password;
        [DataMember]
        public UserElement userElement;
    }
}
