using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
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
    class SearchRosterList : RosterList
    {
        private readonly TextBox _searchTextBox;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SearchRosterList));

        public SearchRosterList(DispatchingCollection<ObservableCollection<Attendance>, Attendance> source, ListBox listBox)
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
        }
        /*
    if (Dispatcher.CheckAccess())
            {
                string searchString = _searchTextBox.Text;
                if (!String.IsNullOrWhiteSpace(searchString))
                {
                    RosterElement rosterElement = filterEventArgs.Item as RosterElement;
                    if (rosterElement != null && rosterElement.Attendance != null &&
                        ((rosterElement.Attendance.Person.Firstname != null &&
                          rosterElement.Attendance.Person.Firstname.StartsWith(searchString, true, CultureInfo.CurrentUICulture)) ||
                         (rosterElement.Attendance.Person.Lastname != null &&
                          rosterElement.Attendance.Person.Lastname.StartsWith(searchString, true, CultureInfo.CurrentUICulture)) ||
                         (rosterElement.Attendance.Person.Username != null &&
                          rosterElement.Attendance.Person.Username.StartsWith(searchString, true, CultureInfo.CurrentUICulture)) ||
                         (rosterElement.Attendance.Person.Email != null &&
                          rosterElement.Attendance.Person.Email.StartsWith(searchString, true, CultureInfo.CurrentUICulture)))
                        )
                    {
                        filterEventArgs.Accepted = true;
                    }
                    else
                    {
                        filterEventArgs.Accepted = false;
                    }
                }
                else
                {
                    filterEventArgs.Accepted = false;
                }
            }
            else
            {
                Dispatcher.Invoke(new Action(() => FilterEventHandler(sender, filterEventArgs)));
            }
        }
         */

    }
}
