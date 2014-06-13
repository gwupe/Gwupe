using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using BlitsMe.Cloud.Messaging.Elements;

namespace BlitsMe.Cloud.Messaging.Response
{
    [DataContract]
    public class UpdateRelationshipRs : API.Response
    {
        public override String type
        {
            get { return "UpdateRelationship-RS"; }
            set { }
        }

        [DataMember] public RelationshipElement relationshipElement;
    }
}
