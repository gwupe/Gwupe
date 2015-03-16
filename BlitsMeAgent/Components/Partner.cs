using System;
using System.ComponentModel;
using Gwupe.Agent.Annotations;

namespace Gwupe.Agent.Components
{
    internal class Partner : INotifyPropertyChanged
    {
        private string _basename;
        private string _text;
        private string _website;
        private string _linkText;
        private string _name;
        private byte[] _logo;
        public event PropertyChangedEventHandler PropertyChanged;

        public String Basename
        {
            get { return _basename; }
            set { _basename = value; OnPropertyChanged("Basename"); }
        }

        public String Text
        {
            get { return _text; }
            set { _text = value; OnPropertyChanged("Text"); }
        }

        public String Website
        {
            get { return _website; }
            set { _website = value; OnPropertyChanged("Website"); }
        }

        public String LinkText
        {
            get { return _linkText; }
            set { _linkText = value; OnPropertyChanged("LinkText"); }
        }

        public String Name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged("Name"); }
        }

        public byte[] Logo
        {
            get { return _logo; }
            set { _logo = value; OnPropertyChanged("Logo"); }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}