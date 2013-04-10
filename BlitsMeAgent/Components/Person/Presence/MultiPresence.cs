using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlitsMe.Agent.Components.Person.Presence
{
    class MultiPresence : IPresence
    {
        private readonly Dictionary<String, IPresence> _presences = new Dictionary<string, IPresence>();

        public string Mode { get { return _presences.Count > 0 ? GetHighestPriorityPresence().Mode : null; } }
        public string Type { get { return _presences.Count > 0 ? GetHighestPriorityPresence().Type : null; } }
        public int Priority { get { return _presences.Count > 0 ? GetHighestPriorityPresence().Priority : 0; } }
        public bool IsOnline { get { return _presences.Count > 0 && GetHighestPriorityPresence().IsOnline; } }
        public bool IsAvailable { get { return _presences.Count > 0 && GetHighestPriorityPresence().IsAvailable; } }
        public string Status { get { return _presences.Count > 0 ? GetHighestPriorityPresence().Status : null; } }

        public void AddPresence(String resource, IPresence presence)
        {
            if (_presences.ContainsKey(resource))
            {
                _presences[resource] = presence;
            } else
            {
                _presences.Add(resource, presence);
            }
        }

        private IPresence GetHighestPriorityPresence()
        {
            var presences = new List<IPresence>(_presences.Values);
            presences.Sort();
            return presences[0];
        }

        public IPresence GetPresence(String resource)
        {
            IPresence presence = null;
            if(_presences.ContainsKey(resource))
            {
                _presences.TryGetValue(resource, out presence);
            }
            return presence;
        }

        public int CompareTo(IPresence other)
        {
            return Priority.CompareTo(other.Priority);
        }
    }
}
