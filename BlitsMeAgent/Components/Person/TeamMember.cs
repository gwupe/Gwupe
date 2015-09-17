using Gwupe.Cloud.Messaging.Elements;

namespace Gwupe.Agent.Components.Person
{
    public class TeamMember : Person
    {
        private bool _admin;
        private bool _member;

        public TeamMember(TeamMemberElement teamMemberElement)
        {
            InitTeamMember(teamMemberElement);
        }

        private void InitTeamMember(TeamMemberElement teamMemberElement)
        {
            InitParty(teamMemberElement);
            Admin = teamMemberElement.admin;
            Member = teamMemberElement.player;
        }

        public bool Admin
        {
            get { return _admin; }
            set
            {
                if (value.Equals(_admin)) return;
                _admin = value;
                OnPropertyChanged("Admin");
            }
        }

        public bool Member
        {
            get { return _member; }
            set
            {
                if (value.Equals(_member)) return;
                _member = value;
                OnPropertyChanged("Member");
            }
        }
    }
}