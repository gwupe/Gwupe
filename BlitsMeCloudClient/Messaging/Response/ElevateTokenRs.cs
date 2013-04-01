using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace BlitsMe.Cloud.Messaging.Response
{
    [DataContract]
    public class ElevateTokenRs : API.Response
    {
        public override string type
        {
            get { return "ElevateToken-RS"; }
            set { }
        }

        [DataMember] public String tokenId;
        [DataMember] public String token;
    }
}
