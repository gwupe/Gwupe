using System.Runtime.Serialization;

namespace BlitsMe.Cloud.Messaging.Elements
{
    [DataContract]
    public class ResultElement
    {
        [DataMember] public UserElement user;
        [DataMember] public bool online;
    }
}