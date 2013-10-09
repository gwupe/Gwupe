using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlitsMe.Agent.Components.Notification;

namespace BlitsMe.Agent.Components
{
    internal class Interactions
    {
        private readonly Engagement _engagement;
        //private Dictionary<String, Interaction> _interactionLookup;
        private readonly List<Interaction> _interactions;
        private object interactionLock = new Object();

        internal Interactions(Engagement engagement)
        {
            _engagement = engagement;
            _interactions = new List<Interaction>();
            //_interactionLookup = new Dictionary<string, Interaction>();
        }

        internal Interaction CurrentInteraction
        {
            get
            {
                lock (interactionLock)
                {
                    if (_interactions.Count == 0 || _interactions[0].Expired) return null;
                    return _interactions[0];
                }
            }
        }

        internal Interaction CurrentOrNewInteraction
        {
            get
            {
                lock (interactionLock)
                {
                    return CurrentInteraction ?? StartInteraction();
                }
            }
        }

        internal Interaction StartInteraction(String id = null)
        {
            lock (interactionLock)
            {
                _interactions.Insert(0, new Interaction(_engagement,id));
                //_interactionLookup.Add(_interactions[0].Id, _interactions[0]);
                return _interactions[0];
            }
        }

        public void Close()
        {
            if (CurrentInteraction != null) CurrentInteraction.Expire();
        }
    }
}
