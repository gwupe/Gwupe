using System;
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
        private static readonly ILog Logger = LogManager.GetLogger(typeof(RosterManager));
        private readonly Thread _rosterManagerThread;
        private readonly BlitsMeClientAppContext _appContext;
        private readonly RosterRq _rosterRequest = new RosterRq();

        public event EventHandler RosterRefreshed;
        public event EventHandler EntriesUpdated;
        public event EventHandler EntriesDeleted;

        public ObservableCollection<Person> ServicePersonList { get; private set; }
        private Dictionary<String,Person> ServicePersonLookup { get; set; }

        public RosterManager(BlitsMeClientAppContext appContext)
        {
            this._appContext = appContext;
            ServicePersonList = new ObservableCollection<Person>();
            ServicePersonLookup = new Dictionary<String, Person>();
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
            if(ServicePersonLookup.ContainsKey(username))
            {
                return ServicePersonLookup[username];
            }
            return null;
        }

        private void Run()
        {
            while (true)
            {
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
                        if(response.rosterElements != null)
                        {
                            ServicePersonList.Clear();
                            ServicePersonLookup.Clear();
                            foreach (RosterElement rosterElement in response.rosterElements)
                            {
                                // Add each buddy to the list
                                Person person = new Person(rosterElement);
                                try
                                {
                                    VCardRs cardRs = (VCardRs) _appContext.ConnectionManager.Connection.Request(new VCardRq(person.Username));
                                    if(cardRs.avatarData != null && !cardRs.avatarData.Equals(""))
                                    {
                                        person.Avatar = Convert.FromBase64String(cardRs.avatarData);
                                    }

                                } catch(Exception e)
                                {
                                    Logger.Error("Failed to get VCard information for " + person.Username + " : " + e.Message,e);
                                }
                                ServicePersonList.Add(person);
                                ServicePersonLookup[person.Username] = person;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Failed to get the Roster : " + e.Message,e);
                    }
                    if (_appContext.ConnectionManager.IsOnline())
                    {
                        Monitor.Wait(_appContext.LoginManager.LogoutOccurredLock);
                    }
                }
            }
        }

        internal void PresenceChange(String user, PresenceElement presence, String shortCode)
        {
            foreach (Person servicePerson in ServicePersonList)
            {
                if(servicePerson.Username.Equals(user))
                {
                    servicePerson.Presence = new Presence(presence);
                    Logger.Info("Presence change, " + user + (servicePerson.Presence.IsAvailable ? " is available " : " is no longer available"));
                    if (shortCode != null)
                    {
                        servicePerson.ShortCode = shortCode;
                    }
                    break;
                }
            }
        }
    }
}
