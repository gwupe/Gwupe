using System;
using System.Runtime.Serialization;

namespace Gwupe.Cloud.Messaging.Elements
{
    [DataContract]
    public class MembershipDeleteElement
    {
        public String uniqueHandle { get; set; }
    }
}