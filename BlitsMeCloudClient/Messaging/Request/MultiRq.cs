using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace BlitsMe.Cloud.Messaging.Request
{
    [DataContract]
    class MultiRq : API.Request
    {
        public override string type { get { return "Multi-RQ"; }
            set { }
        }

        [DataMember] public Lazy<API.Request> requests;
        [DataMember] public bool parallel;
    }
}
