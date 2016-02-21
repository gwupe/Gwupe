using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Gwupe.Cloud.Messaging.Elements
{
    [DataContract]
    public class UserElement : PartyElement
    {
        [DataMember]
        public bool guest;
    }
}
