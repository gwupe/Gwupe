using Gwupe.Cloud.Messaging.Elements;

namespace Gwupe.Agent.Components.Person
{
    public class TeamMember : Person
    {
        private bool _admin;
        private PlayerMembership _player;

        public TeamMember(TeamMemberElement teamMemberElement)
        {
            InitTeamMember(teamMemberElement);
        }

        private void InitTeamMember(TeamMemberElement teamMemberElement)
        {
            InitParty(teamMemberElement);
            Admin = teamMemberElement.admin;
            Player = teamMemberElement.Player;
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

        public PlayerMembership Player
        {
            get { return _player; }
            set
            {
                if (value.Equals(_player)) return;
                _player = value;
                OnPropertyChanged("Player");
            }
        }

        public override string ToString()
        {
            return base.ToString() + " [" + (Admin ? " admin" : " ") + Player + " ]";
        }
    }
}