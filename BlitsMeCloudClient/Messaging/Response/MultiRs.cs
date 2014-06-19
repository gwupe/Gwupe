using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlitsMe.Cloud.Messaging.Response
{
    class MultiRs : API.Response
    {
        public override string type {
            get { return "Multi-RS"; }
            set { }
        }
    }
}
