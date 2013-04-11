using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        private readonly MultiPresence _presence;
        internal IPresence Presence
        {
            get { return _presence; }
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
            this._presence = new MultiPresence();
        }

        public Person(RosterElement rosterElement) : this(rosterElement.userElement)
        {
            // Set backing field directly, no listeners yet
            this.Groups = rosterElement.groups;
            this._shortCode = rosterElement.shortCode;
            _presence.AddPresence(new Presence.Presence("default",rosterElement.presence));
        }

        public Person(UserElement userElement) : this()
        {
            this._name = userElement.name;
            this._username = userElement.user.Split(new char[] {'@'})[0];
            this._email = userElement.email;
            this._location = userElement.location;
            this._rating = userElement.rating;
            this._joined = userElement.joined;
            this._status = userElement.status;
            this._type = userElement.type;
            _firstname = userElement.firstname;
            _lastname = userElement.lastname;
            _description = userElement.description;
            if(userElement.avatarData != null)
            {
                try
                {
                    SetAvatarData(userElement.avatarData);
                }catch (Exception e)
                {
                    Logger.Error("Failed to decode avatar from avatar data",e);
                }
            }
            
        }

        internal void SetPresence(IPresence presence)
        {
            _presence.AddPresence(presence);
            OnPropertyChanged("Presence");
        }

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
