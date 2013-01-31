using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using BlitsMe.Cloud.Messaging.Elements;

namespace BlitsMe.Cloud.Messaging.Request
{
    [DataContract]
    public class ChatMessageRq : BlitsMe.Cloud.Messaging.API.Request
    {
        public override String type
        {
            get { return "ChatMessage-RQ"; }
            set { }
        }

        [DataMember]
        public String from { get; set; }

        [DataMember]
        public String to { get; set; }

        [DataMember]
        public String chatId { get; set; }

        [DataMember]
        public String message { get; set; }

        [DataMember]
        public String fromShortCode { get; set; }
    }
}
