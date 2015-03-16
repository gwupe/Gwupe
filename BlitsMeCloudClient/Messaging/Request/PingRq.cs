using System;
using System.Runtime.Serialization;

namespace Gwupe.Cloud.Messaging.Request
{
    [DataContract]
    public class PingRq : Gwupe.Cloud.Messaging.API.Request
    {
        public override String type  {
            get { return "Ping-RQ"; }
            set { }
        }
    }
}
