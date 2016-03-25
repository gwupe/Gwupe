using System;
using System.Runtime.Serialization;
using Gwupe.Cloud.Messaging.Elements;

namespace Gwupe.Cloud.Messaging.Request
{
    [DataContract]
    public class SubscribeRq : API.Request
    {
        public override string type { get { return "Subscribe-RQ"; } set { } }
        [DataMember] public String username;
        [DataMember] public bool subscribe;
        [DataMember] public bool team;
        [DataMember] public UserElement userElement;
        [DataMember] public TeamElement teamElement;
    }
}
