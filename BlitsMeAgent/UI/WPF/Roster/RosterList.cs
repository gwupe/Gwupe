using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Threading;
using BlitsMe.Agent.Components.Person;
using BlitsMe.Agent.UI.WPF.Utils;
using log4net;

namespace BlitsMe.Agent.UI.WPF.Roster
{
    class RosterList : ObservableListMirror<Person, RosterElement>
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(RosterList));
        private readonly BlitsMeClientAppContext _appContext;
        // This collection is for manipulating the roster for display (sort etc)
        public CollectionViewSource RosterViewSource { get; private set; }

        public RosterList(BlitsMeClientAppContext appContext, Dispatcher dispatcher) : base(dispatcher)
        {
            this._appContext = appContext;
            // Setup the view on this list, so offline people are offline, sorting is correct etc.
            RosterViewSource = new CollectionViewSource {Source = List};
            RosterViewSource.Filter += RosterFilter;
            RosterViewSource.SortDescriptions.Add(new SortDescription("Person.Rating", ListSortDirection.Descending));
        }

        protected override RosterElement CreateNew(Person sourceObject)
        {
            sourceObject.PropertyChanged += RosterItemChanged;
            return new RosterElement(sourceObject);
        }

        private void RosterItemChanged(object sender, PropertyChangedEventArgs eventArgs)
        {
            if(Dispatcher.CheckAccess())
            {
                RosterViewSource.View.Refresh();
            } else
            {
                Dispatcher.Invoke(new Action(() => RosterViewSource.View.Refresh()));
            }
        }

        private void RosterFilter(object sender, FilterEventArgs eventArgs)
        {
            RosterElement rosterElement = eventArgs.Item as RosterElement;
            if (rosterElement != null && rosterElement.Person.Presence.IsOnline)
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
