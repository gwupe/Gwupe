using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gwupe.Cloud.Messaging.API;
using System.Runtime.Serialization;

namespace Gwupe.Cloud.Messaging.Response
{
    [DataContract]
    public class VersionRs : Gwupe.Cloud.Messaging.API.Response
    {
        public String _version = new System.Version().ToString();
        public override String type  {
            get { return "Version-RS"; }
            set { }
        }
        [DataMember]
        public String version
        {
            get { return _version; }
            set { this._version = version; }
        }
    }
}
