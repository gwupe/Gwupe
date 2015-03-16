using System;
using System.Runtime.Serialization;

namespace Gwupe.Cloud.Messaging.Request
{
    [DataContract]
    public class VersionRq : Gwupe.Cloud.Messaging.API.Request
    {
        public override String type  {
            get { return "Version-RQ"; }
            set { }
        }
    }
}
