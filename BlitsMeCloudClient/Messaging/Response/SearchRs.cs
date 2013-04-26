using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using BlitsMe.Cloud.Messaging.Elements;

namespace BlitsMe.Cloud.Messaging.Response
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
