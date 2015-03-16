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
        private readonly RosterRq _rosterRequest = new RosterRq();
        private bool _haveRoster;
        private readonly ConcurrentQueue<PresenceChangeRq> _queuedPresenceChanges;

        internal ObservableCollection<Attendance> ServicePersonAttendanceList { get; private set; }
        private Dictionary<String, Attendance> ServicePersonAttendanceLookup { get; set; }
        internal bool IsClosed { get; private set; }
        internal Attendance CurrentlyEngaged { get; private set; }

        public RosterManager()
        {
            _appContext = GwupeClientAppContext.CurrentAppContext;
            ServicePersonAttendanceList = new ObservableCollection<Attendance>();
            ServicePersonAttendanceLookup = new Dictionary<String, Attendance>();
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
            if (ServicePersonAttendanceLookup.ContainsKey(username))
            {
                return ServicePersonAttendanceLookup[username];
            }
            return null;
        }

        internal void ProcessPresenceChange(PresenceChangeRq request)
        {
            if (_haveRoster)
            {
                ChangePresence(request.user, new Presence(request.resource, request.presence, request.shortCode));
            }
            else
            {
                _queuedPresenceChanges.Enqueue(request);
            }
        }

        private void ChangePresence(String user, Presence presence)
        {
            Logger.Debug("Change Presence Request for " + user);
            if (!ServicePersonAttendanceLookup.ContainsKey(user))
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
                    RosterRs response = _appContext.ConnectionManager.Connection.Request<RosterRq, RosterRs>(_rosterRequest);
                    if (response.rosterElements != null)
                    {
                        foreach (RosterElement rosterElement in response.rosterElements)
                        {
                            // If there are no subscriptions, as far as we are concerned, they are not part of the roster.
                            if (rosterElement.presence != null && "none".Equals(rosterElement.userElement.subscriptionType))
                                continue;
                            // Add each buddy to the list
                            // I think we should not add a default presence, lets see how it goes
                            AddUserElementToList(rosterElement.userElement, rosterElement.relationshipElement);
                            // Now async get the images
                            if (rosterElement.userElement.hasAvatar)
                            {
                                try
                                {
                                    _appContext.ConnectionManager.Connection.RequestAsync<VCardRq, VCardRs>(
                                        new VCardRq(rosterElement.userElement.user), ResponseHandler);
                                }
                                catch (Exception e)
                                {
                                    Logger.Error("Failed to get the vcard for " + rosterElement.userElement.user, e);
                                }
                            }
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
            ServicePersonAttendanceList.Clear();
            ServicePersonAttendanceLookup.Clear();
        }

        private void ResponseHandler(VCardRq vCardRq, VCardRs vCardRs, Exception e)
        {
            if (e == null)
            {
                if (!String.IsNullOrWhiteSpace(vCardRs.userElement.avatarData) && ServicePersonAttendanceLookup.ContainsKey(vCardRq.username))
                {
                    try
                    {
                        ServicePersonAttendanceLookup[vCardRq.username].Person.SetAvatarData(vCardRs.userElement.avatarData);
                    }
                    catch (Exception e1)
                    {
                        Logger.Error("Failed to set avatar data for " + vCardRq.username, e);
                    }
                }
            }
            else
            {
                Logger.Error("Failed to get vcard for " + vCardRq.username, e);
            }
        }


        private void AddUsernameToList(String username, Presence presence)
        {
            try
            {
                VCardRs cardRs = _appContext.ConnectionManager.Connection.Request<VCardRq, VCardRs>(new VCardRq(username));
                AddUserElementToList(cardRs.userElement, cardRs.relationshipElement, presence);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to get VCard information for " + username + " : " + e.Message, e);
            }
        }

        private void AddUserElementToList(UserElement userElement, RelationshipElement relationshipElement, Presence presence = null)
        {
            Attendance attendance = new Attendance(userElement, relationshipElement);
            attendance.PropertyChanged += MarkUnmarkCurrentlyEngaged;
            if (presence != null)
            {
                attendance.SetPresence(presence);
            }
            ServicePersonAttendanceList.Add(attendance);
            ServicePersonAttendanceLookup[attendance.Person.Username] = attendance;
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

        public void AddPerson(Person person)
        {
            // Lets add this person to the roster
            Logger.Debug("Attempting to add " + person + " to " + _appContext.CurrentUserManager.CurrentUser.Username + "'s Team");
            if (ServicePersonAttendanceLookup.ContainsKey(person.Username))
            {
                Logger.Error("Will not add " + person.Username + " to list, he/she already exists");
            }
            else
            {
                _appContext.ConnectionManager.Connection.RequestAsync<SubscribeRq, SubscribeRs>(new SubscribeRq() { username = person.Username, subscribe = true },
                                                                      (request, response, ex) =>
                                                                      ResponseHandler(request, response, ex, person));
            }
        }

        public void AddAdHocPerson(String username, String shortCode)
        {
            VCardRq request = new VCardRq(username);
            try
            {
                var response = _appContext.ConnectionManager.Connection.Request<VCardRq, VCardRs>(request);
                AddAdHocPerson(response.userElement, shortCode);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to add adhoc person " + username,e);
            }
        }

        public void AddAdHocPerson(UserElement userElement, String shortCode)
        {
            Attendance attendance = new Attendance(new Person(userElement),Relationship.NoRelationship);
            var presence = Presence.AlwaysOn;
            presence.Resource = "_blitsme-" + shortCode;
            presence.ShortCode = shortCode;
            attendance.PropertyChanged += MarkUnmarkCurrentlyEngaged;
            attendance.SetPresence(presence);
            ServicePersonAttendanceList.Add(attendance);
            ServicePersonAttendanceLookup[attendance.Person.Username] = attendance;
        }

        internal void UpdateRelationship(String contactUsername, Relationship relationship, String tokenId, String securityKey)
        {
            var response = _appContext.ConnectionManager.Connection
                .Request<UpdateRelationshipRq, UpdateRelationshipRs>(
                    new UpdateRelationshipRq()
                    {
                        relationshipElement = new RelationshipElement()
                        {
                            theyHaveUnattendedAccess = relationship.TheyHaveUnattendedAccess,
                            ihaveUnattendedAccess = relationship.IHaveUnattendedAccess
                        },
                        contactUsername = contactUsername,
                        tokenId = tokenId,
                        securityKey = securityKey
                    });
            if (!ServicePersonAttendanceLookup.ContainsKey(contactUsername))
            {
                Logger.Error("Failed to update relationship to " + contactUsername + ", no such contact.");
            }
            else
            {
                ServicePersonAttendanceLookup[contactUsername].Relationship =
                    new Relationship(response.relationshipElement);
            }
        }

        private void ResponseHandler(SubscribeRq request, SubscribeRs response, Exception e, Person person)
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

        public void RequestContactUpdate(string changeId)
        {
            _appContext.ConnectionManager.Connection.RequestAsync<VCardRq, VCardRs>(
                new VCardRq(changeId), UpdateContactResponseHandler);

        }

        private void UpdateContactResponseHandler(VCardRq vCardRq, VCardRs vCardRs, Exception exception)
        {
            if (exception == null)
            {
                if (ServicePersonAttendanceLookup.ContainsKey(vCardRq.username))
                {
                    try
                    {
                        ServicePersonAttendanceLookup[vCardRq.username].Person.InitPerson(vCardRs.userElement);
                        ServicePersonAttendanceLookup[vCardRq.username].Relationship.InitRelationship(vCardRs.relationshipElement);
                    }
                    catch (Exception e1)
                    {
                        Logger.Error("Failed to update contact information for " + vCardRq.username, e1);
                    }
                }
            }
            else
            {
                Logger.Error("Failed to update contact " + vCardRq.username, exception);
            }
        }

    }
}
