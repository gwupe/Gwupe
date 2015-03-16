using System;
using System.Runtime.Serialization;

namespace Gwupe.Cloud.Messaging.Request
{
    [DataContract]
    public class LookupUserRq : API.Request
    {
        public override string type
        {
            get { return "LookupUser-RQ"; }
            set { }
        }

        [DataMember] public String shortCode { get; set; }
    }
}
