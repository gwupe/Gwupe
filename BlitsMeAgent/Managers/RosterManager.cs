using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using Gwupe.Agent.Components.Person;
using Gwupe.Agent.Components.Person.Presence;
using Gwupe.Cloud.Messaging.Elements;
using Gwupe.Cloud.Messaging.Request;
using Gwupe.Cloud.Messaging.Response;
using log4net;

namespace Gwupe.Agent.Managers
{
    public class RosterManager
    {
        private const int PauseOnRosterFail = 10000;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(RosterManager));
        private readonly GwupeClientAppContext _appContext;
        //private readonly RosterRq _rosterRequest = new RosterRq();
        private bool _haveRoster;
        private readonly ConcurrentQueue<PresenceChangeRq> _queuedPresenceChanges;

        internal ObservableCollection<Attendance> ServicePartyAttendanceList { get; private set; }
        private Dictionary<String, Attendance> ServicePartyAttendanceLookup { get; set; }
        internal bool IsClosed { get; private set; }
        internal Attendance CurrentlyEngaged { get; private set; }

        public RosterManager()
        {
            _appContext = GwupeClientAppContext.CurrentAppContext;
            ServicePartyAttendanceList = new ObservableCollection<Attendance>();
            ServicePartyAttendanceLookup = new Dictionary<String, Attendance>();
            _queuedPresenceChanges = new ConcurrentQueue<PresenceChangeRq>();
            _appContext.LoginManager.LoggedOut += (sender, args) => Reset();
        }

        public void Close()
        {
            if (!IsClosed)
            {
                Logger.Debug("Closing RosterManager");
                _reset();
                IsClosed = true;
            }
        }


        internal Attendance GetServicePersonAttendance(String username)
        {
            if (ServicePartyAttendanceLookup.ContainsKey(username))
            {
                return ServicePartyAttendanceLookup[username];
            }
            return null;
        }

        internal void ProcessPresenceChange(PresenceChangeRq request)
        {
            if (_haveRoster)
            {
                ChangePresence(request.user,
                    new Presence(request.resource, request.presence, request.shortCode)
                    {
                        ClientInfo = new ClientInfo()
                        {
                            Platform = request.clientInfo == null ? null : request.clientInfo.platform,
                            Version = request.clientInfo == null ? null : request.clientInfo.version
                        }
                    }
                    );
            }
            else
            {
                _queuedPresenceChanges.Enqueue(request);
            }
        }

        private void ChangePresence(String user, Presence presence)
        {
            Logger.Debug("Change Presence Request for " + user);
            if (!ServicePartyAttendanceLookup.ContainsKey(user))
            {
                if (PresenceType.unavailable.Equals(presence.Type))
                    return;
                // if we are getting presence alerts (excl unavail), we need to create this user
                AddUsernameToList(user, presence);
            }
            Attendance servicePersonAttendance = _appContext.RosterManager.GetServicePersonAttendance(user);
            if (servicePersonAttendance != null)
            {
                servicePersonAttendance.SetPresence(presence);
                Logger.Debug("Incoming presence change for " + user + " [" + presence + "], resource = " + presence.Resource + ", shortCode = " + presence.ShortCode + ", priority " + presence.Priority);
                Logger.Info("Presence change, now " + user +
                            (servicePersonAttendance.Presence.IsOnline ?
                                " is available " + "[" + servicePersonAttendance.Presence + "], resource " + servicePersonAttendance.Presence.Resource + ", shortCode = " + servicePersonAttendance.Presence.ShortCode + ", priority " + servicePersonAttendance.Presence.Priority :
                                " is no longer available " + "[" + servicePersonAttendance.Presence + "]")
                                );
            }

        }

        internal void RetrieveRoster()
        {
            _haveRoster = false;
            Reset();
            for (int attempts = 0; attempts < 3; attempts++)
            {
                try
                {
                    RosterRs response = _appContext.ConnectionManager.Connection.Request<RosterRq, RosterRs>(new RosterRq());
                    if (response.rosterElements != null)
                    {
                        foreach (RosterElement rosterElement in response.rosterElements)
                        {
                            // If there are no subscriptions, as far as we are concerned, they are not part of the roster.
                            if (rosterElement.presence != null && "none".Equals(rosterElement.PartyElement.subscriptionType))
                                continue;
                            // Add each buddy to the list
                            AddPartyElementToList(rosterElement.PartyElement, rosterElement.relationshipElement);
                        }
                        // Process the queued changes
                        while (_queuedPresenceChanges.Count > 0)
                        {
                            PresenceChangeRq request;
                            if (_queuedPresenceChanges.TryDequeue(out request))
                            {
                                ChangePresence(request.user, new Presence(request.resource, request.presence, request.shortCode));
                            }
                            else
                            {
                                Logger.Error("Failed to dequeue from the saved presence change requests");
                                break;
                            }
                        }
                    }
                    _haveRoster = true;
                    break;
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to get the Roster : " + e.Message, e);
                    // Pause here to try again
                    if (attempts < 3)
                    {
                        Thread.Sleep(PauseOnRosterFail);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        internal void Reset()
        {
            Logger.Debug("Resetting Roster");
            _reset();
        }

        private void _reset()
        {
            ServicePartyAttendanceList.Clear();
            ServicePartyAttendanceLookup.Clear();
        }

        private void AddUsernameToList(String username, Presence presence)
        {
            try
            {
                AddPartyToList(GwupeClientAppContext.CurrentAppContext.PartyManager.GetParty(username), presence);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to add the username " + username + " to the roster : " + e.Message, e);
            }
        }

        private void AddPartyElementToList(PartyElement partyElement, RelationshipElement relationshipElement, Presence presence = null)
        {
            var party = GwupeClientAppContext.CurrentAppContext.PartyManager.AddUpdatePartyFromElement(partyElement);
            // Make sure we update the relationship
            GwupeClientAppContext.CurrentAppContext.RelationshipManager.AddUpdateRelationship(party.Username, new Relationship(relationshipElement));
            // This is to update the party in the background (avatar)
            ThreadPool.QueueUserWorkItem(state => GwupeClientAppContext.CurrentAppContext.PartyManager.GetParty(partyElement.user, true));
            // Now add it to the list
            AddPartyToList(party, presence);
        }

        private void AddPartyToList(Party party, Presence presence)
        {
            var attendance = new Attendance(party);
            attendance.PropertyChanged += MarkUnmarkCurrentlyEngaged;
            if (presence != null)
            {
                attendance.SetPresence(presence);
            }
            ServicePartyAttendanceList.Add(attendance);
            ServicePartyAttendanceLookup[attendance.Party.Username] = attendance;
        }

        private void MarkUnmarkCurrentlyEngaged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName.Equals("IsCurrentlyEngaged"))
            {
                var newCurrent = sender as Attendance;
                if (newCurrent != null && newCurrent.IsCurrentlyEngaged)
                {
                    if (CurrentlyEngaged != newCurrent)
                    {
                        if (CurrentlyEngaged != null)
                        {
                            CurrentlyEngaged.IsCurrentlyEngaged = false;
                        }
                        CurrentlyEngaged = newCurrent;
                    }
                }
            }
        }

        public void SubscribePerson(Person person)
        {
            // Lets add this person to the roster
            Logger.Debug("Attempting to add " + person + " to " + _appContext.CurrentUserManager.CurrentUser.Username + "'s Team");
            if (ServicePartyAttendanceLookup.ContainsKey(person.Username))
            {
                Logger.Error("Will not add " + person.Username + " to list, he/she already exists");
            }
            else
            {
                _appContext.ConnectionManager.Connection.RequestAsync<SubscribeRq, SubscribeRs>(new SubscribeRq() { username = person.Username, subscribe = true },
                                                                      (request, response, ex) =>
                                                                      SubscribePersonResponseHandler(request, response, ex, person));
            }
        }

        private void SubscribePersonResponseHandler(SubscribeRq request, SubscribeRs response, Exception e, Person person)
        {
            if (e == null)
            {
                person.SubscriptionStatus = "subscribe";
                Logger.Debug("Succeeded in sending subscribe request for " + person.Username);
            }
            else
            {
                Logger.Error("Failed to subscribe to " + person.Username + " : " + e.Message, e);
            }
        }

        public void AddAdHocPerson(String username, String shortCode)
        {
            try
            {
                AddAdHocPerson(GwupeClientAppContext.CurrentAppContext.PartyManager.GetParty(username), shortCode);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to add adhoc person " + username, e);
            }
        }

        public void AddAdHocPerson(PartyElement partyElement, String shortCode)
        {
            AddAdHocPerson(GwupeClientAppContext.CurrentAppContext.PartyManager.AddUpdatePartyFromElement(partyElement), shortCode);
        }

        public void AddAdHocPerson(Party party, String shortCode)
        {
            var attendance = new Attendance(party);
            var presence = Presence.AlwaysOn;
            presence.Resource = "_blitsme-" + shortCode;
            presence.ShortCode = shortCode;
            attendance.PropertyChanged += MarkUnmarkCurrentlyEngaged;
            attendance.SetPresence(presence);
            ServicePartyAttendanceList.Add(attendance);
            ServicePartyAttendanceLookup[attendance.Party.Username] = attendance;
        }




    }
}
