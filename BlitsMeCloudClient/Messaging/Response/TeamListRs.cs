using System.Collections.Generic;
using System.Runtime.Serialization;
using Gwupe.Cloud.Messaging.Elements;

namespace Gwupe.Cloud.Messaging.Response
{
    [DataContract]
    public class TeamListRs : API.Response
    {
        public override string type
        {
            get { return "TeamList-RS"; }
            set { }
        }

        [DataMember]
        public List<TeamElement> teams;
    }
}
