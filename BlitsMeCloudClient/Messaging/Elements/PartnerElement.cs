using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace BlitsMe.Cloud.Messaging.Elements
{
    [DataContract]
    public class PartnerElement
    {
        [DataMember]
        public string basename;
        [DataMember]
        public string website;
        [DataMember]
        public string logo;
        [DataMember]
        public string text;
        [DataMember]
        public string linkText;
        [DataMember]
        public string name;
    }
}
