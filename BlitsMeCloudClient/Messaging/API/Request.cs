using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlitsMe.Common.Security;
using System.Runtime.Serialization;

namespace BlitsMe.Cloud.Messaging.API
{
    [DataContract]
    public abstract class Request : Message
    {
        public override abstract String type { get; set; }

        protected Request()
        {
            this.id = Util.getSingleton().generateString(16);
        }
    }
}
