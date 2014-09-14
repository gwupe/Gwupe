using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using BlitsMe.Agent.Components.Person;
using log4net;

namespace BlitsMe.Agent.UI.WPF.Roster
{
    class ActiveRosterList : RosterList
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ActiveRosterList));

        public ActiveRosterList(DispatchingCollection<ObservableCollection<Attendance>, Attendance> source, ListBox listBox)
            : base(source, listBox)
        {
            ContactsView.SortDescriptions.Add(new SortDescription("Attendance.Name", ListSortDirection.Ascending));
        }

        protected override void FilterEventHandler(object sender, FilterEventArgs filterEventArgs)
        {
            Attendance attendance = filterEventArgs.Item as Attendance;
            Logger.Debug("Checking status of " + attendance.Person + " " + attendance.IsActive);
            if (attendance != null && attendance.IsActive)
            {
                filterEventArgs.Accepted = true;
            }
            else
            {
                filterEventArgs.Accepted = false;
            }
        }

    }
}
