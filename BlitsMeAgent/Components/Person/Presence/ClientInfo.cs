using System.ComponentModel;
using Gwupe.Agent.Annotations;

namespace Gwupe.Agent.Components.Person.Presence
{
    internal class ClientInfo : INotifyPropertyChanged
    {
        private string _version;
        private string _platform;

        public string Version
        {
            get { return _version; }
            set
            {
                if (value == _version) return;
                _version = value;
                OnPropertyChanged("Version");
            }
        }

        public string Platform
        {
            get { return _platform; }
            set
            {
                if (value == _platform) return;
                _platform = value;
                OnPropertyChanged("Platform");
            }
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