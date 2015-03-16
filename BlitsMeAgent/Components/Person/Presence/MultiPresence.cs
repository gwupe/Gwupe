using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using log4net;
using log4net.Repository.Hierarchy;

namespace Gwupe.Agent.Components.Person.Presence
{
    class MultiPresence : IPresence
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(MultiPresence));
        private readonly Dictionary<String, IPresence> _presences = new Dictionary<string, IPresence>();
        private readonly object _presenceLock = new Object();
        private IPresence _currentHighestPresence;

        public String ShortCode { get { return UnderlyingPresenceCount > 0 ? GetHighestPriorityPresence().ShortCode : null; } }
        public PresenceMode Mode { get { return UnderlyingPresenceCount > 0 ? GetHighestPriorityPresence().Mode : PresenceMode.available; } }
        public PresenceType Type { get { return UnderlyingPresenceCount > 0 ? GetHighestPriorityPresence().Type : PresenceType.unavailable; } }
        public int Priority { get { return UnderlyingPresenceCount > 0 ? GetHighestPriorityPresence().Priority : 0; } }
        public string Resource { get { return UnderlyingPresenceCount > 0 ? GetHighestPriorityPresence().Resource : ""; } }
        public bool IsOnline { get { return UnderlyingPresenceCount > 0 && GetHighestPriorityPresence().IsOnline; } }
        public bool IsPresent { get { return UnderlyingPresenceCount > 0 && GetHighestPriorityPresence().IsPresent; } }
        public string Status { get { return UnderlyingPresenceCount > 0 ? GetHighestPriorityPresence().Status : null; } }

        public void AddPresence(IPresence presence)
        {
            lock (_presenceLock)
            {
                if (_presences.ContainsKey(presence.Resource))
                {
                    if (presence.Type.Equals(PresenceType.unavailable))
                    {
                        _presences.Remove(presence.Resource);
                    }
                    else
                    {
                        _presences[presence.Resource] = presence;
                    }
                }
                else
                {
                    _presences.Add(presence.Resource, presence);
                }
                _currentHighestPresence = null;
                // cache the result
                GetHighestPriorityPresence();
                // Notify
                OnPropertyChanged("Mode");
                OnPropertyChanged("Status");
                OnPropertyChanged("Type");
                OnPropertyChanged("Proirity");
                OnPropertyChanged("Resource");
            }
        }

        private IPresence GetHighestPriorityPresence()
        {
            lock (_presenceLock)
            {
                if (_currentHighestPresence == null)
                {
                    Logger.Debug("Calculating highest priority presence from " + String.Join(" | ", _presences));
                    if (_presences.Count > 0)
                    {
                        var presences = new List<IPresence>(_presences.Values);
                        // This causes an exception sometimes, we need to capture and send a fault report, so we can try figure it out
                        try
                        {
                            presences.Sort();
                        }
                        catch (Exception e)
                        {
                            try
                            {
                                Logger.Error(
                                    "Caught the multi presence exception we are attempting to understand, logging fault report",
                                    e);
                                GwupeClientAppContext.CurrentAppContext.SubmitFaultReport(new FaultReport()
                                {
                                    UserReport = "MultiPresence error " + String.Join(" | ", _presences.Values)
                                });
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Failed to generate fault report for multi presence error",ex);
                                // throw original exception
                                throw e;
                            }
                        }
                        _currentHighestPresence = presences[0];
                    }
                }
                else
                {
                   // Logger.Debug("Using cached highest priority presence");
                }
                //Logger.Debug("Returning highest priority presence " + _currentHighestPresence);
                return _currentHighestPresence;
            }
            return null;
        }

        /*
        public IPresence GetPresence(String resource)
        {
            lock (_presenceLock)
            {
                IPresence presence = null;
                if (_presences.ContainsKey(resource))
                {
                    _presences.TryGetValue(resource, out presence);
                }
                return presence;
            }
        }
        */

        public int CompareTo(IPresence other)
        {
            return Priority.CompareTo(other.Priority);
        }

        public int UnderlyingPresenceCount
        {
            get { lock (_presenceLock) { return _presences.Count; } }
        }

        public override string ToString()
        {
            return UnderlyingPresenceCount > 0 ? GetHighestPriorityPresence() + " (LogonCount=" + UnderlyingPresenceCount + ")" : "";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
