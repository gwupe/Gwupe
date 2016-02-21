using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Gwupe.Cloud.Messaging.Elements;

namespace Gwupe.Cloud.Messaging.Response
{
    [DataContract]
    public class UpdateTeamRs : API.Response
    {
        public override string type
        {
            get { return "UpdateTeam-RS"; }
            set { }
        }

        [DataMember]
        public TeamElement teamElement;
        [DataMember]
        public List<String> validationErrors;
    }
}
