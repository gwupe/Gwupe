using System;
using System.Runtime.Serialization;

namespace BlitsMe.Cloud.Messaging.Request
{
    public class FileSendRequestRq : API.UserToUserRequest
    {
        public override string type
        {
            get { return "FileSendRequest-RQ"; }
            set { }
        }

        [DataMember]
        public String filename { get; set; }
        [DataMember]
        public String fileSendId { get; set; }
        [DataMember]
        public long fileSize { get; set; }
    }
}
