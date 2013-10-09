namespace BlitsMe.Cloud.Messaging.Request
{
    public class RDPRequestRq : API.UserToUserRequest
    {
        public override string type
        {
            get { return "RDPRequest-RQ"; }
            set { }
        }
    }
}
