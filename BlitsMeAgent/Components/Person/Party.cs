using System;
using System.ComponentModel;
using Gwupe.Agent.Annotations;
using Gwupe.Cloud.Messaging.Elements;
using log4net;

namespace Gwupe.Agent.Components.Person
{
    public enum PartyType
    {
        Team,
        Person
    };

    public abstract class Party : INotifyPropertyChanged
    {

        private static readonly ILog Logger = LogManager.GetLogger(typeof(Party));

        protected Party()
        {
        }

        public Party(PartyElement partyElement)
        {
            InitParty(partyElement);
        }

        public void InitParty(PartyElement partyElement)
        {
            Username = partyElement.user.Split(new char[] { '@' })[0];
            Name = partyElement.name;
            Email = partyElement.email;
            Location = partyElement.location;
            Rating = partyElement.rating;
            Joined = partyElement.joined;
            Supporter = partyElement.supporter;
            Firstname = partyElement.firstname;
            Lastname = partyElement.lastname;
            Description = partyElement.description;
            if (!String.IsNullOrWhiteSpace(partyElement.avatarData))
            {
                try
                {
                    SetAvatarData(partyElement.avatarData);
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to decode avatar from avatar data", e);
                }
            }
        }

        abstract public PartyType PartyType { get; }

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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}