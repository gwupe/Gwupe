using System;
using System.Collections.Generic;
using System.ComponentModel;
using BlitsMe.Cloud.Messaging.Elements;
using log4net;

namespace BlitsMe.Agent.Components.Person
{
    public class Person : INotifyPropertyChanged
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Person));
        private string _name;
        public String Name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged("Name"); }
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

        private DateTime _joined;
        public DateTime Joined
        {
            get { return _joined; }
            set { _joined = value; OnPropertyChanged("Joined"); }
        }

        private Presence _presence;
        public Presence Presence
        {
            get { return _presence; }
            set { _presence = value; OnPropertyChanged("Presence"); }
        }

        public IList<String> Groups { get; set; }
        private string _status;
        public String Status
        {
            get { return _status; }
            set { _status = value; OnPropertyChanged("Status"); }
        }

        private string _type;
        public String Type
        {
            get { return _type; }
            set { _type = value; OnPropertyChanged("Type"); }
        }

        private byte[] _avatar;
        public byte[] Avatar
        {
            get { return _avatar; }
            set { _avatar = value; OnPropertyChanged("Avatar"); }
        }

        private string _shortCode;
        public String ShortCode
        {
            get { return _shortCode; }
            set
            {
                if ((_shortCode != value) && (_shortCode == null || !_shortCode.Equals(value)))
                {
                    _shortCode = value;
                    OnPropertyChanged("ShortCode");
                }
            }
        }


        public Person()
        {

        }

        public Person(RosterElement rosterElement)
        {
            // Set backing field directly, no listeners yet
            this._name = rosterElement.name;
            this._shortCode = rosterElement.shortCode;
            this._username = rosterElement.user.Split(new char[] { '@' })[0];
            this._email = rosterElement.email;
            this._location = rosterElement.location;
            this._rating = rosterElement.rating;
            this._joined = rosterElement.joined;
            this.Groups = rosterElement.groups;
            this._presence = new Presence(rosterElement.presence);
            this._status = rosterElement.status;
            this._type = rosterElement.type;
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
