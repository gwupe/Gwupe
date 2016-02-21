using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Gwupe.Cloud.Messaging.Elements;

namespace Gwupe.Cloud.Messaging.Request
{
    [DataContract]
    public class UpdateTeamRq : API.ElevatedRequestImpl
    {
        public override string type
        {
            get { return "UpdateTeam-RQ"; }
            set { }
        }

        [DataMember]
        public TeamElement teamElement;

        [DataMember] public Boolean? playerRequest;

        [DataMember] public Boolean? admin;
    }
}
