using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace BlitsMe.Cloud.Messaging.Request
{
    public class FileSendRequestResponseRq : API.UserToUserRequest
    {
        public override string type { get { return "FileSendRequestResponse-RQ"; } set { } }

        [DataMember]
        public String fileSendId { get; set; }
        [DataMember]
        public bool accepted { get; set; }
    }
}
