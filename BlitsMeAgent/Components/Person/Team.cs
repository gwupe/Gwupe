using System;
using System.Collections.Generic;
using System.Windows.Documents;
using Gwupe.Cloud.Messaging.Elements;
using log4net;
using log4net.Repository.Hierarchy;

namespace Gwupe.Agent.Components.Person
{
    public class Team : Party
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (Team));
        private bool _admin;
        private PlayerMembership _player;

        private readonly List<TeamMember> _teamMembers = new List<TeamMember>();

        public Boolean Admin
        {
            get { return _admin; }
            set
            {
                if (value.Equals(_admin)) return;
                _admin = value;
                OnPropertyChanged(nameof(Admin));
            }
        }

        public PlayerMembership Player
        {
            get { return _player; }
            set
            {
                if (value.Equals(_player)) return;
                _player = value;
                OnPropertyChanged(nameof(Player));
            }
        }

        private bool? _playerRequest;

        public Boolean PlayerRequest
        {
            get { return _playerRequest ?? _player != PlayerMembership.none; }
            set { _playerRequest = value; }
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
            _teamMembers.Clear();
            Admin = false;
            Player = PlayerMembership.none;
            _playerRequest = null;
            foreach (var teamMemberElement in teamElement.teamMembers)
            {
                var teamMember = new TeamMember(teamMemberElement);
                _teamMembers.Add(teamMember);
                Logger.Debug("Adding team member " + teamMember);
                if (!foundMe && teamMember.Username.Equals(currentUsername))
                {
                    Logger.Debug("Found myself in the team list, adding my membership status " + teamMember);
                    Admin = teamMember.Admin;
                    Player = teamMember.Player;
                    foundMe = true;
                }
            }
        }

        public override PartyType PartyType => PartyType.Team;

    }
}