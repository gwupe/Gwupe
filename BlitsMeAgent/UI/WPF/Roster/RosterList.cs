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
    abstract class RosterList
    {
        private readonly DispatchingCollection<ObservableCollection<Attendance>, Attendance> _rosterCollection;
        private readonly ListBox _listBox;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(RosterList));
        // This collection is for manipulating the roster for display (sort etc)
        internal CollectionViewSource ContactsView { get; private set; }

        protected RosterList(DispatchingCollection<ObservableCollection<Attendance>, Attendance> rosterCollection, ListBox listBox)
        {
            _rosterCollection = rosterCollection;
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

        private void OnAttendancePropertyChange(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (!_rosterCollection.Dispatcher.CheckAccess())
            {
                _rosterCollection.Dispatcher.Invoke(new Action(() => OnAttendancePropertyChange(sender, propertyChangedEventArgs)));
                return;
            }
            if (propertyChangedEventArgs.PropertyName.Equals("Presence"))
                ContactsView.View.Refresh();
        }

        //protected abstract void RosterItemChanged(object sender, PropertyChangedEventArgs eventArgs);
        protected abstract void FilterEventHandler(object sender, FilterEventArgs eventArgs);

    }
}
