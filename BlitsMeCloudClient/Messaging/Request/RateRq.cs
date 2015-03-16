using System.Runtime.Serialization;

namespace Gwupe.Cloud.Messaging.Request
{
    [DataContract]
    public class RateRq : API.Request
    {
        public override string type
        {
            get { return "Rate-RQ"; }
            set { }
        }

        [DataMember] public string username;

        [DataMember] public string interactionId;
        [DataMember] public int rating;
        [DataMember] public string ratingName;


    }
}
