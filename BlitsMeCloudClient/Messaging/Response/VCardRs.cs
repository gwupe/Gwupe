using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using BlitsMe.Cloud.Messaging.Elements;

namespace BlitsMe.Cloud.Messaging.Response
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
        [DataMember] public RelationshipElement relationshipElement;
    }
}
