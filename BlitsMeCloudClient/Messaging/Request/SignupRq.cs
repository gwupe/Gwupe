using System;
using System.Runtime.Serialization;

namespace BlitsMe.Cloud.Messaging.Request
{
    [DataContract]
    public class SignupRq : API.Request
    {
        public override string type
        {
            get { return "Signup-RQ"; }
            set { }
        }

        [DataMember]
        public bool supporter { get; set; }

        [DataMember]
        public string firstname { get; set; }

        [DataMember]
        public string lastname { get; set; }

        [DataMember]
        public String username { get; set; }

        [DataMember]
        public String password { get; set; }

        [DataMember]
        public String location { get; set; }

        [DataMember]
        public String email { get; set; }

    }
}
