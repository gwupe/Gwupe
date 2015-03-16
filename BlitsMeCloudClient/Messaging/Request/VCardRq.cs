using System;
using System.Runtime.Serialization;

namespace Gwupe.Cloud.Messaging.Request
{
    [DataContract]
    public class VCardRq : API.Request
    {
        public VCardRq(String username)
        {
            this.username = username;
        }

        public override String type
        {
            get { return "VCard-RQ"; }
            set { }
        }

        [DataMember] public String username;
    }
}
