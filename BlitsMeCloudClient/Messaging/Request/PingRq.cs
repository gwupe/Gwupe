using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlitsMe.Cloud.Messaging.API;
using System.Runtime.Serialization;

namespace BlitsMe.Cloud.Messaging.Request
{
    [DataContract]
    public class PingRq : BlitsMe.Cloud.Messaging.API.Request
    {
        public override String type  {
            get { return "Ping-RQ"; }
            set { }
        }
    }
}
