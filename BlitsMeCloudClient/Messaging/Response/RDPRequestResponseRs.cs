using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace BlitsMe.Cloud.Messaging.Response
{
    public class RDPRequestResponseRs : API.UserToUserResponse
    {
        public override string type
        {
            get { return "RDPRequestResponse-RS"; }
            set { }
        }
    }
}
