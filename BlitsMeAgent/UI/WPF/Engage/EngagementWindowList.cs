using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Threading;
using Gwupe.Agent.Components;
using Gwupe.Agent.Components.Notification;
using Gwupe.Agent.Components.Person;
using Gwupe.Agent.UI.WPF.Utils;
using log4net;

namespace Gwupe.Agent.UI.WPF.Engage
{
    internal class EngagementWindowList : ObservableListMirror<Engagement,EngagementWindow>
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(EngagementWindowList));
        private readonly GwupeClientAppContext _appContext;
        private readonly DispatchingCollection<ObservableCollection<Notification>, Notification> _notificationList;
        private readonly PropertyChangedEventHandler _propertyChangeHandler;

        public EngagementWindowList(GwupeClientAppContext appContext, 
                        DispatchingCollection<ObservableCollection<Notification>, Notification> notificationList, 
                        PropertyChangedEventHandler propertyChangeHandler, 
                        Dispatcher dispatcher) : base(dispatcher)
        {
            this._appContext = appContext;
            this._notificationList = notificationList;
            _propertyChangeHandler = propertyChangeHandler;
        }

        protected override EngagementWindow CreateNew(Engagement sourceObject)
        {
            var egw = new EngagementWindow(_appContext, _notificationList, sourceObject);
            egw.Engagement.PropertyChanged += _propertyChangeHandler;
            return egw;
        }

        public EngagementWindow GetEngagementWindow(Attendance attendance)
        {
            // Call the engagement manager to get the engagement (which will add it if it doesn't exist and also dynamically create a egw due to the mirroring)
            if(_appContext.EngagementManager.GetNewEngagement(attendance.Person.Username) != null)
            {
                if (ListLookup.ContainsKey(attendance.Person.Username))
                {
                    return ListLookup[attendance.Person.Username];
                }
            }
            return null;
        }
    }
}
