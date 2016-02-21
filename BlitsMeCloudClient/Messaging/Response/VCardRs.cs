using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Gwupe.Cloud.Messaging.Elements;

namespace Gwupe.Cloud.Messaging.Response
{
    [DataContract]
    public class VCardRs : API.Response
    {
        public override String type
        {
            get { return "VCard-RS"; }
            set { }
        }

        [DataMember] public UserElement userElement;
        [DataMember] public TeamElement teamElement;
        [DataMember] public RelationshipElement relationshipElement;

        public PartyElement PartyElement
        {
            get
            {
                return IsTeam() ? (PartyElement)teamElement : userElement;
            }
        }

        public Boolean IsTeam()
        {
            return teamElement != null;
        }
    }
}
