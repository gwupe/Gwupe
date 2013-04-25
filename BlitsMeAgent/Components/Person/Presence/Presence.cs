using System;
using System.ComponentModel;
using BlitsMe.Cloud.Messaging.Elements;

namespace BlitsMe.Agent.Components.Person.Presence
{
    internal class Presence : IPresence
    {
        private PresenceMode _mode;
        private PresenceType _type;
        private int _priority;
        private string _resource;
        private string _status;

        public PresenceMode Mode
        {
            get { return _mode; }
            set { _mode = value; OnPropertyChanged("Mode"); }
        }

        public PresenceType Type
        {
            get { return _type; }
            set { _type = value; OnPropertyChanged("Type"); }
        }

        public int Priority
        {
            get { return _priority; }
            set { _priority = value; OnPropertyChanged("Priority"); }
        }

        public String Status
        {
            get { return _status; }
            set { _status = value; OnPropertyChanged("Status"); }
        }

        public string Resource
        {
            get { return _resource; }
            private set { _resource = value; OnPropertyChanged("Resource"); }
        }

        public Boolean IsOnline
        {
            get { return Type.Equals(PresenceType.available); }
        }

        public Boolean IsPresent
        {
            get { return Type.Equals(PresenceType.available) && (Mode.Equals(PresenceMode.available) || Mode.Equals(PresenceMode.chat)); }
        }


        public Presence(String resource, PresenceElement presenceElement)
        {
            this.Mode = ParsePresenceMode(presenceElement.mode);
            this.Type = ParsePresenceType(presenceElement.type);
            this.Status = presenceElement.status;
            this.Priority = presenceElement.priority < 0 ? 0 : presenceElement.priority;
            this.Resource = resource;
        }

        static PresenceMode ParsePresenceMode(string mode)
        {
            if (PresenceMode.available.ToString().Equals(mode))
                return PresenceMode.available;
            if (PresenceMode.chat.ToString().Equals(mode))
                return PresenceMode.chat;
            if (PresenceMode.away.ToString().Equals(mode))
                return PresenceMode.away;
            if (PresenceMode.xa.ToString().Equals(mode))
                return PresenceMode.xa;
            if (PresenceMode.dnd.ToString().Equals(mode))
                return PresenceMode.dnd;
            throw new Exception("Failed to parse " + mode + " into a presenceMode");
        }

        static PresenceType ParsePresenceType(String presenceType)
        {
            if(PresenceType.available.ToString().Equals(presenceType))
            {
                return PresenceType.available;
            }
            return PresenceType.unavailable;
        }

        public void SetIdleState(IdleState idleState)
        {
            switch (idleState)
            {
                case IdleState.InUse:
                    Type = PresenceType.available;
                    Mode = PresenceMode.available;
                    break;
                case IdleState.Idle:
                    Type = PresenceType.available;
                    Mode = PresenceMode.away;
                    break;
                case IdleState.ExtendedIdle:
                    Type = PresenceType.available;
                    Mode = PresenceMode.xa;
                    break;
            }
        }

        public Presence()
        {
            
        }

        public int CompareTo(IPresence other)
        {
            return other.Type.Equals(PresenceType.unavailable) ? -1 : other.Priority.CompareTo(Priority);
        }

        public override String ToString()
        {
            return Type + "/" + Mode;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
