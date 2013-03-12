using System;
using BlitsMe.Cloud.Messaging.Elements;

namespace BlitsMe.Agent.Components.Person
{
    public class Presence
    {
        public static readonly String AVAILABLE = "AVAILABLE";
        public String Mode;
        public String Type;

        public Boolean IsOnline
        {
            get { return Type.Equals(AVAILABLE); }
        }

        public Boolean IsAvailable
        {
            get { return Type.Equals(AVAILABLE) && Mode.Equals(AVAILABLE); }
        }

        public String Status
        {
            get { return Type.Equals(AVAILABLE) ? Mode : Type; }
        }

        public Presence(PresenceElement presenceElement)
        {
            this.Mode = presenceElement.mode;
            this.Type = presenceElement.type;
        }

    }
}
