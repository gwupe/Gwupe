using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using BlitsMe.Agent.Components.Person;
using BlitsMe.Agent.UI.WPF.Utils;
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
            if (attendance != null && (attendance.IsActive || attendance.IsUnread))
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
