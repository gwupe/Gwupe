using System.Runtime.Serialization;

namespace BlitsMe.Cloud.Messaging.Request
{
    [DataContract]
    public class SearchRq : API.Request
    {
        public override string type
        {
            get { return "Search-RQ"; }
            set { }
        }

        [DataMember] public string query;
        [DataMember] public int page;
        [DataMember] public int pageSize;
    }
}
