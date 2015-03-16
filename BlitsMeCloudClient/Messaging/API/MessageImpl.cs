using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Gwupe.Cloud.Messaging.API
{
    [DataContract]
    public class MessageImpl : Gwupe.Cloud.Messaging.API.Message
    {
        public override String type { get; set; }
    }
}
