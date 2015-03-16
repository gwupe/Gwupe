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
using Gwupe.Agent.Components.Person;
using Gwupe.Agent.UI.WPF.Utils;
using log4net;

namespace Gwupe.Agent.UI.WPF.Roster
{
    abstract class RosterList
    {
        protected readonly DispatchingCollection<ObservableCollection<Attendance>, Attendance> RosterCollection;
        protected readonly ListBox _listBox;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(RosterList));
        // This collection is for manipulating the roster for display (sort etc)
        internal CollectionViewSource ContactsView { get; private set; }

        protected RosterList(DispatchingCollection<ObservableCollection<Attendance>, Attendance> rosterCollection, ListBox listBox)
        {
            RosterCollection = rosterCollection;
            _listBox = listBox;
            // Setup the view on this list, so offline people are offline, sorting is correct etc.
            ContactsView = new CollectionViewSource { Source = rosterCollection };
            ContactsView.Filter += FilterEventHandler;
            rosterCollection.UnderlyingCollection.CollectionChanged += ContactCollectionChanged;
        }

        private void ContactCollectionChanged(object sender, NotifyCollectionChangedEventArgs eventArgs)
        {
            switch (eventArgs.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        if (eventArgs.NewItems != null)
                        {
                            foreach (Attendance newItem in eventArgs.NewItems)
                            {
                                newItem.PropertyChanged += OnAttendancePropertyChange;
                                //GwupeClientAppContext.CurrentAppContext.UIManager.GetContactRating(newItem.Person.Rating);
                            }
                        }
                    }
                    break;
            }
        }

        internal void MarkRoster(Attendance attendance, bool markActive = false)
        {
            var element = _listBox.ItemContainerGenerator.ContainerFromItem(attendance) as ListBoxItem;
            if (element != null && element.IsSelected == false)
            {
                element.IsSelected = true;
            }
        }

        protected virtual void OnAttendancePropertyChange(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            var attendance = sender as Attendance;
            if (attendance != null)
            {
                if (propertyChangedEventArgs.PropertyName.Equals("Presence"))
                {
                    if (!attendance.Presence.IsOnline)
                    {
                        RefreshRoster();
                    }
                }
            }
        }


        public void RefreshRoster()
        {
            if (!RosterCollection.Dispatcher.CheckAccess())
            {
                RosterCollection.Dispatcher.Invoke(new Action(RefreshRoster));
            }
            else
            {
                ContactsView.View.Refresh();
            }
        }

        //protected abstract void RosterItemChanged(object sender, PropertyChangedEventArgs eventArgs);
        protected abstract void FilterEventHandler(object sender, FilterEventArgs eventArgs);

    }
}
