using System;
using System.Runtime.Serialization;

namespace BlitsMe.Cloud.Messaging.Request
{
    [DataContract]
    public class VersionRq : BlitsMe.Cloud.Messaging.API.Request
    {
        public override String type  {
            get { return "Version-RQ"; }
            set { }
        }
    }
}
