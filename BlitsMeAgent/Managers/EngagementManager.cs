using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using BlitsMe.Agent.Components;
using BlitsMe.Agent.Components.Person;
using log4net;

namespace BlitsMe.Agent.Managers
{
    internal delegate void EngagementActivityEvent(object sender, EngagementActivity args);

    class EngagementManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(EngagementManager));
        private readonly BlitsMeClientAppContext _appContext;
        private readonly Dictionary<String, Engagement> _engagementLookup = new Dictionary<string, Engagement>();
        private readonly object _engagementLookupLock = new object();
        internal bool IsClosed { get; private set; }
        public ObservableCollection<Engagement> Engagements { get; private set; }

        internal event EngagementActivityEvent NewActivity;

        public EngagementManager()
        {
            _appContext = BlitsMeClientAppContext.CurrentAppContext;
            Engagements = new ObservableCollection<Engagement>();
            _appContext.LoginManager.LoggedOut += (sender, args) => Reset();
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

                Attendance servicePersonAttendance = _appContext.RosterManager.GetServicePersonAttendance(username.ToLower());
                if (servicePersonAttendance == null)
                {
                    throw new Exception("Unable to find service person [" + username + "]");
                }
                var newEngagement = new Engagement(_appContext, servicePersonAttendance);
                newEngagement.Functions.Values.ToList().ForEach(value => value.NewActivity += (sender, args) => OnActivity(args));
                Engagements.Add(newEngagement);
                _engagementLookup[username.ToLower()] = newEngagement;
                servicePersonAttendance.Engagement = newEngagement;
                return _engagementLookup[username.ToLower()];
            }
        }

        public void OnActivity(EngagementActivity args)
        {
            args.Engagement.ActivityOccured(args);
            if (_appContext.IsShuttingDown) return;
            EngagementActivityEvent handler = NewActivity;
            if (handler != null) handler(this, args);
        }

        public void Reset()
        {
            Logger.Debug("Resetting Engagement Manager, clearing engagements");
            _reset();
        }

        private void _reset()
        {
            foreach (Engagement engagement in Engagements)
            {
                engagement.Close();
            }
            Engagements.Clear();
            _engagementLookup.Clear();
        }

        public void Close()
        {
            if (!IsClosed)
            {
                IsClosed = true;
                Logger.Debug("Closing Engagement Manager");
                _reset();
            }
        }

    }
}
