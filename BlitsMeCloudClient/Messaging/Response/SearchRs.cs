using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Gwupe.Cloud.Messaging.Elements;

namespace Gwupe.Cloud.Messaging.Response
{
    [DataContract]
    public class SearchRs : API.Response
    {
        public override string type
        {
            get { return "SearchRs"; }
            set { }
        }

        [DataMember] public List<ResultElement> results;
        [DataMember] public int totalResults;
    }
}
