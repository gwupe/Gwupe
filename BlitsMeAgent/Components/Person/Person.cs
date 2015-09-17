using System;
using System.Collections.Generic;
using Gwupe.Cloud.Messaging.Elements;
using log4net;

namespace Gwupe.Agent.Components.Person
{
    public class Person : Party
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Person));
        private bool _guest;
        public bool Guest
        {
            set { _guest = value; OnPropertyChanged("Guest"); }
            get { return _guest; }
        }

        public IList<String> Groups { get; set; }
        private string _subscriptionStatus;
        public String SubscriptionStatus
        {
            get { return _subscriptionStatus; }
            set { _subscriptionStatus = value; OnPropertyChanged("SubscriptionStatus"); }
        }

        private string _subscriptionType;
        public String SubscriptionType
        {
            get { return _subscriptionType; }
            set { _subscriptionType = value; OnPropertyChanged("SubscriptionType"); }
        }

        public Person(RosterElement rosterElement)
            : this(rosterElement.userElement)
        {
            // Set backing field directly, no listeners yet
            this.Groups = rosterElement.groups;
        }

        public Person(UserElement userElement)
        {
            InitPerson(userElement);
        }

        public void InitPerson(UserElement userElement)
        {
            InitParty(userElement);
            SubscriptionStatus = userElement.subscriptionStatus;
            SubscriptionType = userElement.subscriptionType;
            Guest = userElement.guest;
        }

        internal Person() { }

        public override PartyType PartyType
        {
            get { return PartyType.Person; }
        }
    }
}
