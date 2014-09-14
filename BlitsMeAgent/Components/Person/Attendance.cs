using System;
using System.ComponentModel;
using System.Threading;
using BlitsMe.Agent.Components.Activity;
using BlitsMe.Agent.Components.Person.Presence;
using BlitsMe.Cloud.Messaging.Elements;
using log4net;
using System.Windows.Input;

namespace BlitsMe.Agent.Components.Person
{
    internal class Attendance : INotifyPropertyChanged
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Attendance));

        public Person Person { get; set; }

        private readonly MultiPresence _presence;
        public IPresence Presence
        {
            get { return _presence; }
        }

        internal Attendance()
        {
            this._presence = new MultiPresence();
            Person = new Person();
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

        private Relationship _relationship;
        public Relationship Relationship
        {
            get { return _relationship; }
            set { _relationship = value; OnPropertyChanged("Relationship"); }
        }

        internal Attendance(RosterElement element)
        {
            Person = new Person(element);
            this._presence = new MultiPresence();
        }

        internal Attendance(UserElement element, RelationshipElement relationshipElement) : this(new Person(element), new Relationship(relationshipElement))
        {
        }

        internal Attendance(Person person, Relationship relationship)
        {
            Person = person;
            Relationship = relationship;
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
            return Person.ToString();
        }
    }
}
