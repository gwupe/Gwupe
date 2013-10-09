using System;
using System.Runtime.Serialization;
using BlitsMe.Cloud.Messaging.API;

namespace BlitsMe.Cloud.Messaging.Request
{
    [DataContract]
    public class ChatMessageRq : UserToUserRequest
    {
        public override String type
        {
            get { return "ChatMessage-RQ"; }
            set { }
        }

        [DataMember]
        public String chatId { get; set; }

        [DataMember]
        public String message { get; set; }
    }
}
