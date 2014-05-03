using System;
using System.Runtime.Serialization;

namespace BlitsMe.Cloud.Messaging.Request
{
    public class InitRepeatedConnectionRq : API.UserToUserRequest
    {
        public override string type
        {
            get { return "InitRepeatedConnection-RQ"; }
            set { }
        }

        [DataMember] public String repeatId;
        [DataMember] public String function;
    }
}
