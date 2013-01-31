using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace BlitsMe.Cloud.Messaging.API
{
    [DataContract]
    public class MessageImpl : BlitsMe.Cloud.Messaging.API.Message
    {
        public override String type { get; set; }
    }
}
