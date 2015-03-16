using System.Runtime.Serialization;

namespace Gwupe.Cloud.Messaging.Elements
{
    [DataContract]
    public class ResultElement
    {
        [DataMember] public UserElement user;
        [DataMember] public bool online;
    }
}