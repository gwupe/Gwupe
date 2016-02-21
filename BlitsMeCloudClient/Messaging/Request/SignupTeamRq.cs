using System;
using System.Runtime.Serialization;

namespace Gwupe.Cloud.Messaging.Request
{
    [DataContract]
    public class SignupTeamRq : API.Request
    {
        public override string type
        {
            get { return "SignupTeam-RQ"; }
            set { }
        }

        [DataMember]
        public bool supporter { get; set; }

        [DataMember]
        public string teamName { get; set; }

        [DataMember]
        public String uniqueHandle { get; set; }

        [DataMember]
        public String location { get; set; }

        [DataMember]
        public String email { get; set; }

    }
}
