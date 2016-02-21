using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Gwupe.Cloud.Messaging.Request
{
    [DataContract]
    public class TeamListRq : API.Request
    {
        public override string type
        {
            get { return "TeamList-RQ"; }
            set { }
        }

        [DataMember]
        public bool includeAvatar { get; set; }
    }
}
