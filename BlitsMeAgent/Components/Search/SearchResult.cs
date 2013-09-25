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
            _person = new Person.Person(resultElement.user);
            _person.PropertyChanged += PersonOnPropertyChanged;
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
            get { return "Add".Equals(AddText); }
        }

        public String AddText
        {
            get
            {
                if ("subscribe".Equals(_person.SubscriptionStatus))
                {
                    return "Pending Add";
                }
                if ("both".Equals(_person.SubscriptionType))
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