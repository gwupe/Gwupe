using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Gwupe.Cloud.Messaging.Request
{
    [DataContract]
    public class NotifyChangeRq : API.Request
    {
        public const String CHANGE_TYPE_MOD = "MOD";
        public const String OBJECT_TYPE_USER = "USER";

        public override String type
        {
            get { return "NotifyChange-RQ"; }
            set { }
        }

        [DataMember]
        public String changeType;
        [DataMember]
        public String changeObject;
        [DataMember]
        public String changeId;
    }
}
