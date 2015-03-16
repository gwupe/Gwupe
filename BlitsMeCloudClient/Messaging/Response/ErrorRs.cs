using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Gwupe.Cloud.Messaging.Response
{
    [DataContract]
    public class ErrorRs : Gwupe.Cloud.Messaging.API.Response
    {
        [DataMember]
        public override string type
        {
            get
            {
                return "Error-RS";
            }
            set
            { 
            }
        }
    }
}
