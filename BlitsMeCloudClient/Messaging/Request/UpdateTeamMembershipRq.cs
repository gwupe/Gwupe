using System.Collections.Generic;
using System.Runtime.Serialization;
using Gwupe.Cloud.Messaging.Elements;

namespace Gwupe.Cloud.Messaging.Request
{
    [DataContract]
    public class UpdateTeamMembershipRq : API.Request
    {
        public override string type
        {
            get { return "UpdateTeamMembership-RQ"; }
            set { }
        }

        [DataMember]
        public List<MembershipUpdateElement> updateMembers;
        [DataMember]
        public List<MembershipDeleteElement> deleteMembers;
    }
}