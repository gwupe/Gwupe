using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

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
    }
}
