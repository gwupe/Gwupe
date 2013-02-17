using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace BlitsMe.Cloud.Messaging.Request
{
    public class RDPRequestRq : API.UserToUserRequest
    {
        public override string type
        {
            get { return "RDPRequest-RQ"; }
            set { }
        }
    }
}
