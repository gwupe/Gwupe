using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace BlitsMe.Cloud.Messaging.API
{
    [DataContract]
    public abstract class Response : Message
    {
        public override abstract String type { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string error { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string errorMessage { get; set; }
        public virtual bool isValid()
        {
            return error == null && errorMessage == null;
        }
    }
}
