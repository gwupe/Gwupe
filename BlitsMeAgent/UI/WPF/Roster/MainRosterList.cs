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
            ContactsView.SortDescriptions.Add(new SortDescription("Person.Name", ListSortDirection.Ascending));
        }

        protected override void OnAttendancePropertyChange(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            var attendance = sender as Attendance;
            if (attendance != null)
            {
                if (propertyChangedEventArgs.PropertyName.Equals("Presence"))
                {
                    if (!attendance.Presence.IsOnline)
                    {
                        // we fade him
                        RosterCollection.Dispatcher.Invoke(new Action(() => FadeRosterElement(attendance)));
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
            var listItem = _listBox.ItemContainerGenerator.ContainerFromItem(attendance) as ListBoxItem;
            if (listItem != null)
            {
                //var animation = new DoubleAnimation(1.0, 0.0, new TimeSpan(0, 0, 0, 1, 300));
                var animation2 = new DoubleAnimation(65, 0, new TimeSpan(0, 0, 0, 0, 1000))
                {
                    FillBehavior = FillBehavior.Stop,
                    BeginTime = new TimeSpan(0, 0, 0, 0, 3000),
                    AccelerationRatio = 0.3,
                    DecelerationRatio = 0.6,
                };
                //animation.Completed += (sender, args) => rosterElement.BeginAnimation(Grid.HeightProperty, animation2);
                animation2.Completed += (o, args) =>
                {
                    listItem.BeginAnimation(UIElement.OpacityProperty, null);
                    RefreshRoster();
                };
                Logger.Debug("Fading element");
                //listItem.BeginAnimation(UIElement.OpacityProperty, animation);
                listItem.BeginAnimation(FrameworkElement.HeightProperty, animation2);

            }
        }

        protected override void FilterEventHandler(object sender, FilterEventArgs eventArgs)
        {
            Attendance attendance = eventArgs.Item as Attendance;
            if (attendance != null && attendance.Presence != null && attendance.Presence.IsOnline && !attendance.IsActive && !attendance.IsUnread && !attendance.Person.Guest)
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
