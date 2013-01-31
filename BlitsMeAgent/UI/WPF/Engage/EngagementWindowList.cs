using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using BlitsMe.Agent.Components;
using BlitsMe.Agent.Components.Notification;
using BlitsMe.Agent.Components.Person;
using BlitsMe.Agent.UI.WPF.Utils;
using log4net;

namespace BlitsMe.Agent.UI.WPF.Engage
{
    class EngagementWindowList : ObservableListMirror<Engagement,EngagementWindow>
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(EngagementWindowList));
        private readonly BlitsMeClientAppContext _appContext;
        private readonly DispatchingCollection<ObservableCollection<INotification>, INotification> _notificationList;

        public EngagementWindowList(BlitsMeClientAppContext appContext, DispatchingCollection<ObservableCollection<INotification>,INotification> notificationList, Dispatcher dispatcher) : base(dispatcher)
        {
            this._appContext = appContext;
            this._notificationList = notificationList;
        }

        protected override EngagementWindow CreateNew(Engagement sourceObject)
        {
            return new EngagementWindow(_appContext, _notificationList, sourceObject);
        }

        public EngagementWindow GetEngagementWindow(Person person)
        {
            // Call the engagement manager to get the engagement (which will add it if it doesn't exist and also dynamically create a egw due to the mirroring)
            if(_appContext.EngagementManager.GetNewEngagement(person.Username) != null)
            {
                if (ListLookup.ContainsKey(person.Username))
                {
                    return ListLookup[person.Username];
                }
            }
            return null;
        }
    }
}
