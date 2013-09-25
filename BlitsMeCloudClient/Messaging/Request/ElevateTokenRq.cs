using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace BlitsMe.Cloud.Messaging.Request
{
    [DataContract]
    public class ElevateTokenRq : API.Request
    {
        public override string type
        {
            get { return "ElevateToken-RQ"; }
            set { }
        }
    }
}
