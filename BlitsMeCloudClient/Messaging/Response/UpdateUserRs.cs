using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Gwupe.Cloud.Messaging.Elements;

namespace Gwupe.Cloud.Messaging.Response
{
    [DataContract]
    public class UpdateUserRs : API.Response
    {
        public override string type
        {
            get { return "UpdateUser-RS"; }
            set { }
        }

        [DataMember] public UserElement userElement;
        [DataMember] public List<String> validationErrors;
    }
}
