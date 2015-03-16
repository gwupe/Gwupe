using System.Runtime.Serialization;

namespace Gwupe.Cloud.Messaging.Elements
{
    [DataContract]
    public class RelationshipElement
    {
        [DataMember] public bool theyHaveUnattendedAccess;
        [DataMember] public bool ihaveUnattendedAccess;
    }
}