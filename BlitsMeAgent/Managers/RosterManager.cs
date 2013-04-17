using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using BlitsMe.Agent.Components.Person;
using BlitsMe.Agent.Components.Person.Presence;
using BlitsMe.Cloud.Messaging.API;
using BlitsMe.Cloud.Messaging.Elements;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using log4net;

namespace BlitsMe.Agent.Managers
{
    public class RosterManager
    {
        private const int PauseOnRosterFail = 10000;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(RosterManager));
        private readonly Thread _rosterManagerThread;
        private readonly BlitsMeClientAppContext _appContext;
        private readonly RosterRq _rosterRequest = new RosterRq();
        private bool _haveRoster;
        private readonly ConcurrentQueue<PresenceChangeRq> _queuedPresenceChanges;

        public event EventHandler RosterRefreshed;
        public event EventHandler EntriesUpdated;
        public event EventHandler EntriesDeleted;

        public ObservableCollection<Person> ServicePersonList { get; private set; }
        private Dictionary<String, Person> ServicePersonLookup { get; set; }

        public RosterManager(BlitsMeClientAppContext appContext)
        {
            this._appContext = appContext;
            ServicePersonList = new ObservableCollection<Person>();
            ServicePersonLookup = new Dictionary<String, Person>();
            _queuedPresenceChanges = new ConcurrentQueue<PresenceChangeRq>();
            // Manager thread
            _rosterManagerThread = new Thread(Run) { IsBackground = true, Name = "_rosterManagerThread" };
            _rosterManagerThread.Start();
        }

        public void Close()
        {
            _rosterManagerThread.Abort();
        }

        public Person GetServicePerson(String username)
        {
            if (ServicePersonLookup.ContainsKey(username))
            {
                return ServicePersonLookup[username];
            }
            return null;
        }

        internal void ProcessPresenceChange(PresenceChangeRq request)
        {
            if (_haveRoster)
            {
                ChangePresence(request.user, request.shortCode, new Presence(request.resource, request.presence));
            }
            else
            {
                _queuedPresenceChanges.Enqueue(request);
            }
        }

        private void ChangePresence(String user, String shortCode, Presence presence)
        {
            if(!ServicePersonLookup.ContainsKey(user)){
                if (Presence.UNAVAILABLE.Equals(presence.Type))
                    return;
                // if we are getting presence alerts (excl unavail), we need to create this user
                AddUsernameToList(user, presence);
            }
            Person servicePerson = _appContext.RosterManager.GetServicePerson(user);
            if (servicePerson != null)
            {
                servicePerson.SetPresence(presence);
                Logger.Debug("Incoming presence change for " + user + " [" + presence + "], resource = " + presence.Resource + ", priority " + presence.Priority);
                Logger.Info("Presence change, now " + user +
                            (servicePerson.Presence.IsOnline ?
                                " is available " + "[" + servicePerson.Presence + "], resource " + servicePerson.Presence.Resource + ", priority " + servicePerson.Presence.Priority :
                                " is no longer available " + "[" + servicePerson.Presence + "]")
                                );
                if (shortCode != null)
                {
                    servicePerson.ShortCode = shortCode;
                }
            }

        }


        private void Run()
        {
            while (true)
            {
                _haveRoster = false;
                ServicePersonList.Clear();
                ServicePersonLookup.Clear();
                lock (_appContext.LoginManager.LoginOccurredLock)
                {
                    if (!_appContext.ConnectionManager.IsOnline())
                    {
                        Monitor.Wait(_appContext.LoginManager.LoginOccurredLock);
                    }
                }
                lock (_appContext.LoginManager.LogoutOccurredLock)
                {
                    // Lets get the Roster
#if DEBUG
                    Logger.Debug("Retrieving the Roster");
#endif
                    try
                    {
                        RosterRs response = _appContext.ConnectionManager.Connection.Request<RosterRq,RosterRs>(_rosterRequest);
                        if (response.rosterElements != null)
                        {
                            foreach (RosterElement rosterElement in response.rosterElements)
                            {
                                // If there are no subscriptions, as far as we are concerned, they are not part of the roster.
                                if (rosterElement.presence != null && "none".Equals(rosterElement.userElement.subscriptionType))
                                    continue;
                                // Add each buddy to the list
                                // I think we should not add a default presence, lets see how it goes
                                AddUserElementToList(rosterElement.userElement);
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
                            while(_queuedPresenceChanges.Count > 0)
                            {
                                PresenceChangeRq request;
                                if(_queuedPresenceChanges.TryDequeue(out request))
                                {
                                    ChangePresence(request.user,  request.shortCode, new Presence(request.resource, request.presence));
                                } else
                                {
                                    Logger.Error("Failed to dequeue from the saved presence change requests");
                                    break;
                                }
                            }
                        }
                        _haveRoster = true;
                        if (_appContext.ConnectionManager.IsOnline())
                        {
                            Monitor.Wait(_appContext.LoginManager.LogoutOccurredLock);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Failed to get the Roster : " + e.Message, e);
                        // Pause here to try again
                        Thread.Sleep(PauseOnRosterFail);
                    }
                }
            }
        }

        private void ResponseHandler(VCardRq vCardRq, VCardRs vCardRs, Exception e)
        {
            if(e == null)
            {
                if(!String.IsNullOrWhiteSpace(vCardRs.userElement.avatarData) && ServicePersonLookup.ContainsKey(vCardRq.username))
                {
                    try
                    {
                        ServicePersonLookup[vCardRq.username].SetAvatarData(vCardRs.userElement.avatarData);
                    }catch (Exception e1)
                    {
                        Logger.Error("Failed to set avatar data for " + vCardRq.username,e);
                    }
                }
            } else
            {
                Logger.Error("Failed to get vcard for " + vCardRq.username,e);
            }
        }


        private void AddUsernameToList(String username, Presence presence)
        {
            try
            {
                VCardRs cardRs = _appContext.ConnectionManager.Connection.Request<VCardRq,VCardRs>(new VCardRq(username));
                AddUserElementToList(cardRs.userElement, presence);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to get VCard information for " + username + " : " + e.Message, e);
            }
        }

        private void AddUserElementToList(UserElement userElement, Presence presence = null)
        {
            Person person = new Person(userElement);
            if (presence != null)
            {
                person.SetPresence(presence);
            }
            ServicePersonList.Add(person);
            ServicePersonLookup[person.Username] = person;
        }

        public void AddPerson(Person person)
        {
            // Lets add this person to the roster
            Logger.Debug("Attempting to add " + person + " to " + _appContext.LoginManager.LoginDetails.username + "'s Team");
            if(ServicePersonLookup.ContainsKey(person.Username))
            {
                Logger.Error("Will not add " + person.Username + " to list, he/she already exists");
            } else
            {
                _appContext.ConnectionManager.Connection.RequestAsync<SubscribeRq,SubscribeRs>(new SubscribeRq() { username = person.Username, subscribe = true },
                                                                      (request, response, ex) =>
                                                                      ResponseHandler(request, response, ex, person));
            }
        }

        private void ResponseHandler(SubscribeRq request, SubscribeRs response, Exception e, Person person)
        {
            if(e == null)
            {
                person.SubscriptionStatus = "subscribe";
                Logger.Debug("Succeeded in sending subscribe request for " + person.Username);
            } else
            {
                Logger.Error("Failed to subscribe to " + person.Username + " : " + e.Message,e);
            }
        }
    }
}
