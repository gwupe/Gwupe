using System;
using System.Collections.Generic;
using System.Windows.Documents;
using Gwupe.Cloud.Messaging.Elements;

namespace Gwupe.Agent.Components.Person
{
    public class Team : Party
    {
        private bool _admin;
        private bool _player;

        private readonly List<TeamMember> _teamMembers = new List<TeamMember>();

        public Boolean Admin
        {
            get { return _admin; }
            set
            {
                if (value.Equals(_admin)) return;
                _admin = value;
                OnPropertyChanged("Admin");
            }
        }

        public Boolean Player
        {
            get { return _player; }
            set
            {
                if (value.Equals(_player)) return;
                _player = value;
                OnPropertyChanged("Player");
            }
        }

        public Team(TeamElement teamElement)
        {
            InitTeam(teamElement);
        }

        public void InitTeam(TeamElement teamElement)
        {
            InitParty(teamElement);
            bool foundMe = false;
            var currentUsername = GwupeClientAppContext.CurrentAppContext.CurrentUserManager.CurrentUser.Username;
            foreach (var teamMemberElement in teamElement.teamMembers)
            {
                var teamMember = new TeamMember(teamMemberElement);
                _teamMembers.Add(teamMember);
                if (!foundMe && teamMember.Username.Equals(currentUsername))
                {
                    Admin = teamMember.Admin;
                    Player = teamMember.Member;
                    foundMe = true;
                }
            }
        }

        public override PartyType PartyType
        {
            get { return PartyType.Team; }
        }
    }
}