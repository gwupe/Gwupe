using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Gwupe.Cloud.Messaging.Response
{
    [DataContract]
    public class SignupTeamRs : API.Response
    {
        public override string type { get { return "SignupTeam-RS"; } set { } }

        [DataMember] public List<String> signupErrors;
    }
}
