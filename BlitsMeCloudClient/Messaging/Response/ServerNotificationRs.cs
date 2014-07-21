using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlitsMe.Cloud.Messaging.API;
using System.Runtime.Serialization;

namespace BlitsMe.Cloud.Messaging.Response
{
    [DataContract]
    public class ServerNotificationRs : API.Response
    {
        public override String type  {
            get { return "ServerNotification-RS"; }
            set { }
        }
    }
}
