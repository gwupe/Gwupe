using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace BlitsMe.Agent.Components.Person.Presence
{
    class MultiPresence : IPresence
    {
        private readonly Dictionary<String, IPresence> _presences = new Dictionary<string, IPresence>();
        private readonly object _presenceLock = new Object();

        public String ShortCode { get { return _presences.Count > 0 ? GetHighestPriorityPresence().ShortCode : null; } }
        public PresenceMode Mode { get { return _presences.Count > 0 ? GetHighestPriorityPresence().Mode : PresenceMode.available; } }
        public PresenceType Type { get { return _presences.Count > 0 ? GetHighestPriorityPresence().Type : PresenceType.unavailable; } }
        public int Priority { get { return _presences.Count > 0 ? GetHighestPriorityPresence().Priority : 0; } }
        public string Resource { get { return _presences.Count > 0 ? GetHighestPriorityPresence().Resource : ""; } }
        public bool IsOnline { get { return _presences.Count > 0 && GetHighestPriorityPresence().IsOnline; } }
        public bool IsPresent { get { return _presences.Count > 0 && GetHighestPriorityPresence().IsPresent; } }
        public string Status { get { return _presences.Count > 0 ? GetHighestPriorityPresence().Status : null; } }

        public void AddPresence(IPresence presence)
        {
            lock (_presenceLock)
            {
                if (_presences.ContainsKey(presence.Resource))
                {
                    if (presence.Type.Equals(PresenceType.unavailable))
                    {
                        _presences.Remove(presence.Resource);
                    }
                    else
                    {
                        _presences[presence.Resource] = presence;
                    }
                }
                else
                {
                    _presences.Add(presence.Resource, presence);
                }
                OnPropertyChanged("Mode");
                OnPropertyChanged("Status");
                OnPropertyChanged("Type");
                OnPropertyChanged("Proirity");
                OnPropertyChanged("Resource");
            }
        }

        private IPresence GetHighestPriorityPresence()
        {
            lock (_presenceLock)
            {
                if (_presences.Count > 0)
                {
                    var presences = new List<IPresence>(_presences.Values);
                    presences.Sort();
                    return presences[0];
                }
            }
            return null;
        }

        public IPresence GetPresence(String resource)
        {
            lock (_presenceLock)
            {
                IPresence presence = null;
                if (_presences.ContainsKey(resource))
                {
                    _presences.TryGetValue(resource, out presence);
                }
                return presence;
            }
        }

        public int CompareTo(IPresence other)
        {
            return Priority.CompareTo(other.Priority);
        }

        public int UnderlyingPresenceCount
        {
            get { return _presences.Count; }
        }

        public override string ToString()
        {
            return _presences.Count > 0 ? GetHighestPriorityPresence() + " (LogonCount=" + _presences.Count + ")" : "";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
