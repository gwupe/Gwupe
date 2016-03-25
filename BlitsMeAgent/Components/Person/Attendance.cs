using System;
using System.ComponentModel;
using System.Threading;
using Gwupe.Agent.Components.Activity;
using Gwupe.Agent.Components.Person.Presence;
using Gwupe.Cloud.Messaging.Elements;
using log4net;
using System.Windows.Input;

namespace Gwupe.Agent.Components.Person
{

    internal class Attendance : INotifyPropertyChanged
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Attendance));

        public Party Party { get; set; }

        private readonly MultiPresence _presence;
        public IPresence Presence
        {
            get { return _presence; }
        }

        private bool _isRemoteActive = false;
        public bool IsRemoteActive
        {
            get { return _isRemoteActive; }
            set
            {
                _isRemoteActive = value;
                Logger.Debug("IsRemoteActive is " + _isRemoteActive);
                OnPropertyChanged("IsRemoteActive");
            }
        }

        internal Attendance(UserElement element) 
            : this(new Person(element))
        {
        }

        internal Attendance(TeamElement element)
            : this(new Team(element))
        {
        }

        internal Attendance(Party party)
        {
            Party = party;
            this._presence = new MultiPresence();
        }

        private string _activeShortCode;
        private Engagement _engagement;
        
        private bool _isCurrentlyEngaged;

        internal String ActiveShortCode
        {
            get
            {
                if (_activeShortCode == null)
                {
                    ActiveShortCode = Presence.ShortCode;
                }
                return _activeShortCode;
            }
            set
            {
                if ((_activeShortCode != value) && (_activeShortCode == null || !_activeShortCode.Equals(value)))
                {
                    _activeShortCode = value;
                    OnPropertyChanged("ActiveShortCode");
                }
            }
        }

        public Engagement Engagement
        {
            get { return _engagement; }
            set
            {
                _engagement = value;
                _engagement.Activate += (sender, args) => OnPropertyChanged("IsActive");
                _engagement.Deactivate += (sender, args) => OnPropertyChanged("IsActive");
                _engagement.PropertyChanged += EngagementOnPropertyChanged;
                OnPropertyChanged("Engagement");
            }
        }

        private void EngagementOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName.Equals("IsUnread"))
                OnPropertyChanged("IsUnread");
            else if (propertyChangedEventArgs.PropertyName.Equals("Active"))
                OnPropertyChanged("IsActive");
        }

        public bool IsUnread
        {
            get { return (_engagement != null && _engagement.IsUnread); }
        }

        public bool IsActive
        {
            get
            {
                return (_engagement != null && Engagement.Interactions.CurrentInteraction != null);
            }
        }

        public bool IsCurrentlyEngaged
        {
            get { return _isCurrentlyEngaged; }
            set
            {
                if (_isCurrentlyEngaged != value)
                {
                    _isCurrentlyEngaged = value;
                    if (_isCurrentlyEngaged && IsUnread)
                    {
                        Engagement.ActivityOccured(new InteractionActivity(Engagement, InteractionActivity.READ));
                        Engagement.IsUnread = false;
                    }
                    //Logger.Debug(Person.Username + " is currently " + (_isCurrentlyEngaged ? "engaged" : "unengaged"));
                    OnPropertyChanged("IsCurrentlyEngaged");
                }
            }
        }

        internal void SetPresence(IPresence presence)
        {
            _presence.AddPresence(presence);
            if (!Presence.IsOnline)
            {
                ActiveShortCode = null;
            }
            OnPropertyChanged("Presence");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return Party.ToString();
        }
    }
}
