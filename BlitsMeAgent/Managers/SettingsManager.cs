using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using BlitsMe.Agent.Annotations;
using BlitsMe.Agent.Components;
using BlitsMe.Communication.P2P.P2P.Tunnel;

namespace BlitsMe.Agent.Managers
{
    internal class SettingsManager : INotifyPropertyChanged
    {
        internal List<SyncType> SyncTypes = new List<SyncType> { SyncType.All };
        private Partner _partner;

        public Partner Partner
        {
            get { return _partner; }
            set { _partner = value; OnPropertyChanged("Partner"); }
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
