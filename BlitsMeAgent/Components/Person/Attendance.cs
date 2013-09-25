using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using BlitsMe.Agent.Components.Functions.Chat;
using BlitsMe.Agent.Components.Person.Presence;
using BlitsMe.Cloud.Messaging.Elements;

namespace BlitsMe.Agent.Components.Person
{
    internal class Attendance : INotifyPropertyChanged
    {
        public Person Person { get; set; }

        private readonly MultiPresence _presence;
        internal IPresence Presence
        {
            get { return _presence; }
        }

        internal Attendance()
        {
            this._presence = new MultiPresence();
            Person = new Person();
        }

        internal Attendance(RosterElement element)
        {
            Person = new Person(element);
            this._presence = new MultiPresence();
        }

        internal Attendance(UserElement element)
        {
            Person = new Person(element);
            this._presence = new MultiPresence();
        }

        private string _activeShortCode;
        private Engagement _engagement;
        private bool _isCurrentlyEngaged;

        internal String ActiveShortCode
        {
            get { return _activeShortCode; }
            set
            {
                if ((_activeShortCode != value) && (_activeShortCode == null || !_activeShortCode.Equals(value)))
                {
                    _activeShortCode = value;
                    OnPropertyChanged("ActiveShortCode");
                }
            }
        }

        internal Engagement Engagement
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
                OnPropertyChanged(propertyChangedEventArgs.PropertyName);
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
                return (_engagement != null && Engagement.Active);
            }
        }

        public bool IsCurrentlyEngaged
        {
            get { return _isCurrentlyEngaged; }
            set { _isCurrentlyEngaged = value; OnPropertyChanged("IsCurrentlyEngaged"); }
        }

        internal void SetPresence(IPresence presence)
        {
            _presence.AddPresence(presence);
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
