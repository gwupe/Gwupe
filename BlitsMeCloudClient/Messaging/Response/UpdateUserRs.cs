using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using BlitsMe.Cloud.Messaging.Elements;

namespace BlitsMe.Cloud.Messaging.Response
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
    }
}
