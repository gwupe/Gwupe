using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Gwupe.Agent.Components;
using Gwupe.Agent.Components.Person;
using Gwupe.Cloud.Messaging.Request;
using Gwupe.Cloud.Messaging.Response;
using log4net;

namespace Gwupe.Agent.Managers
{
    internal delegate void EngagementActivityEvent(object sender, EngagementActivity args);

    class EngagementManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(EngagementManager));
        private readonly GwupeClientAppContext _appContext;
        private readonly Dictionary<String, Engagement> _engagementLookup = new Dictionary<string, Engagement>();
        private readonly object _engagementLookupLock = new object();
        internal bool IsClosed { get; private set; }
        public ObservableCollection<Engagement> Engagements { get; private set; }

        internal event EngagementActivityEvent NewActivity;

        public EngagementManager()
        {
            _appContext = GwupeClientAppContext.CurrentAppContext;
            Engagements = new ObservableCollection<Engagement>();
            _appContext.LoginManager.LoggedOut += (sender, args) => Reset();
        }

        // Gets an engagement, null if not there
        public Engagement GetEngagement(String username)
        {
            if (_engagementLookup.ContainsKey(username.ToLower()))
            {
                return _engagementLookup[username.ToLower()];
            }
            return null;
        }

        // Gets an engagement, creates it if its not there
        public Engagement GetNewEngagement(String username, String shortCode = null)
        {
            lock (_engagementLookupLock)
            {
                if (_engagementLookup.ContainsKey(username.ToLower()))
                {
                    return _engagementLookup[username.ToLower()];
                }

                Attendance servicePersonAttendance = _appContext.RosterManager.GetServicePersonAttendance(username.ToLower());
                if (servicePersonAttendance == null && shortCode != null)
                {
                    // add them temporarily
                    _appContext.RosterManager.AddAdHocPerson(username, shortCode);
                    servicePersonAttendance = _appContext.RosterManager.GetServicePersonAttendance(username.ToLower());
                }
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

        public String EngageRemoteAccessId(string shortCode)
        {
            LookupUserRq request = new LookupUserRq() { shortCode = shortCode };
            try
            {
                var response = _appContext.ConnectionManager.Connection.Request<LookupUserRq, LookupUserRs>(request);
                Logger.Debug("Got user " + response.userElement.user + " for shortcode " + shortCode);
                if (_appContext.RosterManager.GetServicePersonAttendance(response.userElement.user.ToLower()) == null)
                {
                    // we aren't subscribed to this user, we will need to add a temporary link
                    _appContext.RosterManager.AddAdHocPerson(response.userElement, shortCode);
                }
                return response.userElement.user;
            }
            catch (Exception e)
            {
                Logger.Error("Failed to execute", e);
            }
            return null;
        }
    }
}
