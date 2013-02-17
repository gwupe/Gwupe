using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace BlitsMe.Cloud.Messaging.Response
{
    [DataContract]
    public class ErrorRs : BlitsMe.Cloud.Messaging.API.Response
    {
        public ErrorRs()
        {
        }

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
