using System;
using System.Runtime.Serialization;
using Gwupe.Cloud.Messaging.API;

namespace Gwupe.Cloud.Messaging.Request
{
    public enum ServerNotificationCode { INVALID_SESSION };

    [DataContract]
    public class ServerNotificationRq : API.Request
    {
        public override String type
        {
            get { return "ServerNotification-RQ"; }
            set { }
        }

        [DataMember]
        public String code { get; set; }

        [DataMember]
        public String body { get; set; }
    }
}
