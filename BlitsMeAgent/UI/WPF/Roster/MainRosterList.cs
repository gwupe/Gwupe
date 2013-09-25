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
    class MainRosterList : RosterList
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(MainRosterList));

        public MainRosterList(DispatchingCollection<ObservableCollection<Attendance>, Attendance> source, ListBox listBox)
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

        private void FadeRosterElement(Attendance attendance)
        {
            var rosterElement = GetElement(attendance.Person.Username);
            if (ContactsView.View.Contains(rosterElement))
            {
                DoubleAnimation animation = new DoubleAnimation(1.0, 0.0, new TimeSpan(0, 0, 0, 1, 300));
                DoubleAnimation animation2 = new DoubleAnimation(65, 0, new TimeSpan(0, 0, 0, 0, 800))
                {
                    FillBehavior = FillBehavior.Stop,
                    BeginTime = new TimeSpan(0, 0, 0, 0, 500)
                };
                //animation.Completed += (sender, args) => rosterElement.BeginAnimation(Grid.HeightProperty, animation2);
                animation2.Completed += (o, args) =>
                {
                    rosterElement.BeginAnimation(UIElement.OpacityProperty, null);
                    RefreshRoster();
                };
                Logger.Debug("Fading element");
                rosterElement.BeginAnimation(UIElement.OpacityProperty, animation);
                rosterElement.BeginAnimation(FrameworkElement.HeightProperty, animation2);
            }
        }

        */
        protected override void FilterEventHandler(object sender, FilterEventArgs eventArgs)
        {
            Attendance attendance = eventArgs.Item as Attendance;
            if (attendance != null && attendance.Presence != null && attendance.Presence.IsOnline && !attendance.IsActive)
            {
                eventArgs.Accepted = true;
            }
            else
            {
                eventArgs.Accepted = false;
            }
        }

    }
}
