using System;
using BlitsMe.Cloud.Messaging.Elements;

namespace BlitsMe.Agent.Components.Person.Presence
{
    internal class Presence : IPresence
    {
        public static readonly String AVAILABLE = "AVAILABLE";
        public static readonly String UNAVAILABLE = "UNAVAILABLE";
        public String Mode { get; set; }
        public String Type { get; set; }
        public int Priority { get; set; }
        public string Resource { get; private set; }

        public Boolean IsOnline
        {
            get { return Type.Equals(AVAILABLE); }
        }

        public Boolean IsPresent
        {
            get { return Type.Equals(AVAILABLE) && Mode.Equals(AVAILABLE); }
        }

        public String Status { get; set; }

        public Presence(String resource, PresenceElement presenceElement)
        {
            this.Mode = presenceElement.mode;
            this.Type = presenceElement.type;
            this.Status = presenceElement.status;
            this.Priority = presenceElement.priority < 0 ? 0 : presenceElement.priority;
            this.Resource = resource;
        }

        public int CompareTo(IPresence other)
        {
            return other.Type.Equals(UNAVAILABLE) ? -1 : other.Priority.CompareTo(Priority);
        }

        public override String ToString()
        {
            return Type + "/" + Mode;
        }

    }
}
