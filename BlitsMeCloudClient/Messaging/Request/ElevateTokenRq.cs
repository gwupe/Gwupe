using System.Runtime.Serialization;

namespace BlitsMe.Cloud.Messaging.Request
{
    [DataContract]
    public class ElevateTokenRq : API.Request
    {
        public override string type
        {
            get { return "ElevateToken-RQ"; }
            set { }
        }
    }
}
