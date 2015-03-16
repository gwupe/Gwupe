using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Gwupe.Cloud.Messaging.Response
{
    [DataContract]
    public class ListenHandshakeRs : Gwupe.Cloud.Messaging.API.UserToUserResponse
    {
        public override String type
        {
            get { return "ListenHandshake-RS"; }
            set { }
        }
    }
}
