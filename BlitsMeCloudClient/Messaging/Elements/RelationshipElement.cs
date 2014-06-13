using System.Runtime.Serialization;

namespace BlitsMe.Cloud.Messaging.Elements
{
    [DataContract]
    public class RelationshipElement
    {
        [DataMember] public bool theyHaveUnattendedAccess;
        [DataMember] public bool ihaveUnattendedAccess;
    }
}