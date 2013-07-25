using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using BlitsMe.Agent.Components;
using BlitsMe.Agent.Components.Chat;
using BlitsMe.Agent.Components.Person;
using log4net;

namespace BlitsMe.Agent.Managers
{
    internal delegate void EngagementActivityEvent(object sender, EngagementActivityArgs args);

    class EngagementManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(EngagementManager));
        private readonly BlitsMeClientAppContext _appContext;
        private readonly Dictionary<String, Engagement> _engagementLookup = new Dictionary<string, Engagement>();
        private readonly object _engagementLookupLock = new object();
        public ObservableCollection<Engagement> Engagements { get; private set; }

        internal event EngagementActivityEvent NewActivity;

        public EngagementManager(BlitsMeClientAppContext appContext)
        {
            this._appContext = appContext;
            Engagements = new ObservableCollection<Engagement>();
        }

        // Gets an engagement, null if not there
        public Engagement GetEngagement(String username)
        {
            if(_engagementLookup.ContainsKey(username))
            {
                return _engagementLookup[username];
            }
            return null;
        }

        // Gets an engagement, creates it if its not there
        public Engagement GetNewEngagement(String username)
        {
            lock (_engagementLookupLock)
            {
                if (_engagementLookup.ContainsKey(username.ToLower()))
                {
                    return _engagementLookup[username.ToLower()];
                }

                Person servicePerson = _appContext.RosterManager.GetServicePerson(username.ToLower());
                if (servicePerson == null)
                {
                    throw new Exception("Unable to find service person [" + username + "]");
                }
                var newEngagement = new Engagement(_appContext, servicePerson);
                newEngagement.Chat.NewMessage += OnChatEvent;
                Engagements.Add(newEngagement);
                _engagementLookup[username.ToLower()] = newEngagement;
                return _engagementLookup[username.ToLower()];
            }
        }

        private void OnChatEvent(object sender, ChatEventArgs args)
        {
            OnActivity(args);
        }

        public void OnActivity(EngagementActivityArgs args)
        {
            EngagementActivityEvent handler = NewActivity;
            if (handler != null) handler(this, args);
        }

        public void Reset()
        {
            Logger.Debug("Resetting Engagement Manager, clearing engagements");
            foreach (Engagement engagement in Engagements)
            {
                engagement.Close();
            }
            Engagements.Clear();
            _engagementLookup.Clear();
        }

        public void Close()
        {
            Reset();
        }
    }
}
