using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Gwupe.Cloud.Messaging.Elements;

namespace Gwupe.Cloud.Messaging.Request
{
    [DataContract]
    public class UpdateRelationshipRq : API.ElevatedRequestImpl
    {
        public override String type
        {
            get { return "UpdateRelationship-RQ"; }
            set { }
        }

        [DataMember] public RelationshipElement relationshipElement;
        [DataMember] public String contactUsername;
    }
}
