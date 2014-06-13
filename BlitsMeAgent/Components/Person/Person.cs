using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using BlitsMe.Agent.Components.Person.Presence;
using BlitsMe.Cloud.Messaging.Elements;
using log4net;

namespace BlitsMe.Agent.Components.Person
{
    public class Person : INotifyPropertyChanged
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Person));
        private string _firstname;
        public String Firstname
        {
            get { return _firstname; }
            set { _firstname = value; OnPropertyChanged("Firstname"); }
        }

        private string _lastname;
        public String Lastname
        {
            get { return _lastname; }
            set { _lastname = value; OnPropertyChanged("Lastname"); }
        }

        private string _name;
        public String Name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged("Name"); }
        }

        private string _description;
        public String Description
        {
            get { return _description; }
            set { _description = value; OnPropertyChanged("Description"); }
        }

        private string _username;
        public string Username
        {
            get { return _username; }
            set { _username = value; OnPropertyChanged("Username"); }
        }

        private string _email;
        public String Email
        {
            get { return _email; }
            set { _email = value; OnPropertyChanged("Email"); }
        }

        private string _location;
        public String Location
        {
            get { return _location; }
            set { _location = value; OnPropertyChanged("Location"); }
        }

        private int _rating;
        public int Rating
        {
            get { return _rating; }
            set { _rating = value; OnPropertyChanged("Rating"); }
        }

        private DateTime? _joined;
        public DateTime? Joined
        {
            get { return _joined; }
            set { _joined = value; OnPropertyChanged("Joined"); }
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

        private byte[] _avatar;
        public byte[] Avatar
        {
            get { return _avatar; }
            set { _avatar = value; OnPropertyChanged("Avatar"); }
        }



        private bool _supporter;

        public bool Supporter
        {
            get { return _supporter; }
            set { _supporter = value; OnPropertyChanged("Supporter"); }
        }



        public Person(RosterElement rosterElement) : this(rosterElement.userElement)
        {
            // Set backing field directly, no listeners yet
            this.Groups = rosterElement.groups;
            //this._activeShortCode = rosterElement.shortCode;
            //_presence.AddPresence(new Presence.Presence("default",rosterElement.presence));
        }

        public Person(UserElement userElement)
        {
            InitPerson(userElement);
        }

        public void InitPerson(UserElement userElement)
        {
            Name = userElement.name;
            Username = userElement.user.Split(new char[] { '@' })[0];
            Email = userElement.email;
            Location = userElement.location;
            Rating = userElement.rating;
            Joined = userElement.joined;
            SubscriptionStatus = userElement.subscriptionStatus;
            SubscriptionType = userElement.subscriptionType;
            Supporter = userElement.supporter;
            Firstname = userElement.firstname;
            Lastname = userElement.lastname;
            Description = userElement.description;
            if (!String.IsNullOrWhiteSpace(userElement.avatarData))
            {
                try
                {
                    SetAvatarData(userElement.avatarData);
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to decode avatar from avatar data", e);
                }
            }
        }

        internal Person() {}

        public void SetAvatarData(string avatarData)
        {
            Avatar = avatarData == null ? null : Convert.FromBase64String(avatarData);
        }

        public String GetAvatarData()
        {
            return _avatar == null ? null : Convert.ToBase64String(_avatar);
        }

        public override string ToString()
        {
            return this._name + ", " + _location + ", " + _email + ", " + _rating;
        }

        private void OnPropertyChanged(String propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
