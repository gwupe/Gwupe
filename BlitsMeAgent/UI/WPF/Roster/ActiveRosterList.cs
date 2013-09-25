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

        /*
        protected override void RosterItemChanged(object sender, PropertyChangedEventArgs eventArgs)
        {
            var attendance = sender as Attendance;
            if (attendance != null)
            {
                Logger.Debug("Roster has changed, " + attendance.Person.Username + "'s " + eventArgs.PropertyName + " has changed.");
                if (eventArgs.PropertyName.Equals("Presence"))
                {
                    if (!attendance.Presence.IsOnline)
                    {
                        // we fade him
                        Dispatcher.Invoke(new Action(() => FadeRosterElement(attendance)));
                    }
                    else
                    {
                        RefreshRoster();
                    }
                }
            }

        }

        private void FadeRosterElement(Attendance person)
        {
            var rosterElement = GetElement(person.Person.Username);
            if (ContactsView.View.Contains(rosterElement))
            {
                DoubleAnimation animation = new DoubleAnimation(1.0, 0.5, new TimeSpan(0, 0, 0, 1, 300)) { FillBehavior = FillBehavior.Stop };
                animation.Completed += (o, args) => RefreshRoster();
                Logger.Debug("Fading element");
                rosterElement.BeginAnimation(UIElement.OpacityProperty, animation);
            }
        }
        */

        protected override void FilterEventHandler(object sender, FilterEventArgs filterEventArgs)
        {
            Attendance attendance = filterEventArgs.Item as Attendance;
            if (attendance != null && attendance.IsActive)
            {
                Logger.Debug(attendance.Person.Username + " will be shown");
                filterEventArgs.Accepted = true;
            }
            else
            {
                Logger.Debug(attendance.Person.Username + " will NOT be shown");
                filterEventArgs.Accepted = false;
            }
        }

    }
}
