using System;
using System.Runtime.Serialization;

namespace Gwupe.Cloud.Messaging.API
{
    public interface IElevatedRequest
    {
        [DataMember]
        String tokenId { get; set; }
        [DataMember]
        String securityKey { get; set; }
    }
}