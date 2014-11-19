using System;
using System.ComponentModel;
using BlitsMe.Agent.Components.Notification;
using BlitsMe.Cloud.Messaging.Elements;

namespace BlitsMe.Agent.Components.Search
{
    public class SearchResult : INotifyPropertyChanged
    {
        private readonly Person.Person _person;

        public SearchResult(ResultElement resultElement)
        {
            if (resultElement.user.name != null)
            {
                _person = new Person.Person(resultElement.user);
                _person.PropertyChanged += PersonOnPropertyChanged;
            }
        }

        private void PersonOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if("SubscriptionStatus".Equals(propertyChangedEventArgs.PropertyName))
            {
                OnPropertyChanged("IsContact");
                OnPropertyChanged("IsNotKnown");
                OnPropertyChanged("IsPendingContact");
                OnPropertyChanged("AddText");
            }
        }

        public Person.Person Person
        {
            get { return _person; }
        }

        public string Username
        {
            get { return _person.Username; }
        }

        public bool IsContact
        {
            get { return "both".Equals(_person.SubscriptionType); }
        }

        public bool IsPendingContact
        {
            get { return "subscribe".Equals(_person.SubscriptionStatus); }
        }

        public bool IsNotKnown
        {
            get { return !IsPendingContact && !IsContact; }
        }

        public String AddText
        {
            get
            {
                if (IsPendingContact)
                {
                    return "Pending Add";
                }
                if (IsContact)
                {
                    return "Added";
                }
                return "Add";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}