using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Gwupe.Cloud.Messaging.Elements
{
    [DataContract]
    public class TeamElement : PartyElement
    {
        private List<TeamMemberElement> _teamMembers;

        [DataMember]
        public List<TeamMemberElement> teamMembers
        {
            get { return _teamMembers ?? (_teamMembers = new List<TeamMemberElement>()); }
            set { _teamMembers = value; }
        }

    
    }
}
