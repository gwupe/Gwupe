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
                OnPropertyChanged("CanAdd");
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

        public bool CanAdd
        {
            get { return "ADD".Equals(AddText); }
        }

        public String AddText
        {
            get
            {
                if ("subscribe".Equals(_person.SubscriptionStatus))
                {
                    return "PENDING ADD";
                }
                if ("both".Equals(_person.SubscriptionType))
                {
                    return "ADDED";
                }
                return "ADD";
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