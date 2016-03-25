using System;

namespace Gwupe.Agent.Components.Notification
{
    class JoinTeamNotification : TrueFalseNotification
    {
        private string _teamUsername;
        // Class exists so that a datatemplate can be selected based on type
        public String TeamUsername
        {
            get { return _teamUsername; }
            set { _teamUsername = value; OnPropertyChanged("TeamUsername"); }
        }
    }
}