using System;
using System.Runtime.Serialization;

namespace Gwupe.Cloud.Messaging.Elements
{
    public enum PlayerMembership
    {
        none,
        invited,
        player
    }

    [DataContract]
    public class TeamMemberElement : PartyElement
    {
        public PlayerMembership Player;

        [DataMember]
        public bool admin { get; set; }

        [DataMember]
        internal string player
        {
            get { return Enum.GetName(typeof(PlayerMembership), Player); }
            set
            {
                if (!Enum.TryParse(value, out Player))
                    Player = PlayerMembership.none;
            }
        }
    }
}