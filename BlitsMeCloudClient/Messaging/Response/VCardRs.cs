using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace BlitsMe.Cloud.Messaging.Response
{
    [DataContract]
    public class VCardRs : API.Response
    {
        public override String type
        {
            get { return "VCard-RS"; }
            set { }
        }

        [DataMember]
        public String firstname;
        [DataMember]
        public String surname;
        [DataMember]
        public String avatarData;

    }
}
