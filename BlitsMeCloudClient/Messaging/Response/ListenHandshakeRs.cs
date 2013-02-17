using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace BlitsMe.Cloud.Messaging.Response
{
    [DataContract]
    public class ListenHandshakeRs : BlitsMe.Cloud.Messaging.API.UserToUserResponse
    {
        public override String type
        {
            get { return "ListenHandshake-RS"; }
            set { }
        }
    }
}
