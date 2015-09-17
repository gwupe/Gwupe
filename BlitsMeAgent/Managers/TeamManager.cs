using System;
using System.Collections.ObjectModel;
using Gwupe.Agent.Components.Person;
using Gwupe.Cloud.Messaging.Request;
using Gwupe.Cloud.Messaging.Response;
using log4net;
using log4net.Repository.Hierarchy;

namespace Gwupe.Agent.Managers
{
    internal class TeamManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (TeamManager));
        internal ObservableCollection<Team> Teams;

        internal TeamManager()
        {
            Teams = new ObservableCollection<Team>();
        }

        public void RetrieveTeams()
        {
            var request = new TeamListRq();
            try
            {
                var response =
                    GwupeClientAppContext.CurrentAppContext.ConnectionManager.Connection.Request<TeamListRq, TeamListRs>
                        (request);
                if (response.teams != null)
                {
                    foreach (var team in response.teams)
                    {
                        var newTeam = new Team(team);
                        this.Teams.Add(newTeam);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to get the team", e);
                throw e;
            }
        }
    }
}
