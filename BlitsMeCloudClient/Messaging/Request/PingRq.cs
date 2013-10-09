using System;
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
