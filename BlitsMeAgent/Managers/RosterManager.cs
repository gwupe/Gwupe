using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using BlitsMe.Agent.Components.Person;
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
                ChangePresence(request.user, request.shortCode, new Presence(request.presence));
            }
            else
            {
                _queuedPresenceChanges.Enqueue(request);
            }
        }

        private void ChangePresence(String user, String shortCode, Presence presence)
        {
            Person servicePerson = _appContext.RosterManager.GetServicePerson(user);
            if (servicePerson != null)
            {
                servicePerson.Presence = presence;
                Logger.Info("Presence change, " + user +
                            (servicePerson.Presence.IsAvailable ? " is available " : " is no longer available") + "[" + servicePerson.Presence.Status + "]");
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
                        RosterRs response = (RosterRs)_appContext.ConnectionManager.Connection.Request(_rosterRequest);
                        if (response.rosterElements != null)
                        {
                            foreach (RosterElement rosterElement in response.rosterElements)
                            {
                                // Add each buddy to the list
                                Person person = new Person(rosterElement);
                                try
                                {
                                    VCardRs cardRs = (VCardRs)_appContext.ConnectionManager.Connection.Request(new VCardRq(person.Username));
                                    if (cardRs.avatarData != null && !cardRs.avatarData.Equals(""))
                                    {
                                        person.Avatar = Convert.FromBase64String(cardRs.avatarData);
                                    }

                                }
                                catch (Exception e)
                                {
                                    Logger.Error("Failed to get VCard information for " + person.Username + " : " + e.Message, e);
                                }
                                ServicePersonList.Add(person);
                                ServicePersonLookup[person.Username] = person;
                            }
                            // Process the queued changes
                            while(_queuedPresenceChanges.Count > 0)
                            {
                                PresenceChangeRq request;
                                if(_queuedPresenceChanges.TryDequeue(out request))
                                {
                                    ChangePresence(request.user, request.shortCode, new Presence(request.presence));
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

    }
}
