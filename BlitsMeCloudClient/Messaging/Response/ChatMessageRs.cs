using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlitsMe.Cloud.Messaging.API;
using System.Runtime.Serialization;

namespace BlitsMe.Cloud.Messaging.Response
{
    [DataContract]
    public class ChatMessageRs : BlitsMe.Cloud.Messaging.API.Response
    {
        public override String type  {
            get { return "ChatMessage-RS"; }
            set { }
        }
    }
}
