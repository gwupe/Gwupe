using System.Collections.ObjectModel;
using Gwupe.Agent.Components.Person;

namespace Gwupe.Agent.Managers
{
    internal class TeamManager
    {
        internal ObservableCollection<Team> Teams;

        internal TeamManager()
        {
            Teams = new ObservableCollection<Team>();
        }

        public void SetTeams(Collection<Team> teams)
        {
            Teams = new ObservableCollection<Team>(teams);
        }
    }
}
