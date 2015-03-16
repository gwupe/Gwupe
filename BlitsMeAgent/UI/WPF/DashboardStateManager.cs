using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Gwupe.Agent.Annotations;
using log4net;
using log4net.Repository.Hierarchy;

namespace Gwupe.Agent.UI.WPF
{
    
    public enum DashboardState
    {
        Default,
        Elevate,
        Alert,
        UserInputPrompt,
        FaultReport,
    };

    public class DashboardStateManager : INotifyPropertyChanged
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(DashboardStateManager));
        public DashboardState DashboardState { get { return _dashboardState.Peek(); } }
        public event PropertyChangedEventHandler PropertyChanged;
        private Stack<DashboardState> _dashboardState;
        private List<DashboardState> _dashboardStateRequests;
        private readonly object _lock = new object();

        public DashboardStateManager()
        {
            Reset();
        }

        private void Reset()
        {
            lock (_lock)
            {
                _dashboardState = new Stack<DashboardState>();
                _dashboardState.Push(DashboardState.Default);
                _dashboardStateRequests = new List<DashboardState>();
            }
            OnPropertyChanged("DashboardState");
        }


        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public void EnableDashboardState(DashboardState state)
        {
            lock (_lock)
            {
                _dashboardState.Push(state);
                Logger.Debug("Enabled " + _dashboardState.Peek() + ", " + _dashboardState.Count + " states now");
            }
            OnPropertyChanged("DashboardState");
        }

        public void DisableDashboardState(DashboardState state)
        {
            lock (_lock)
            {
                if (_dashboardState.Contains(state))
                {
                    _dashboardStateRequests.Add(state);
                    while (_dashboardStateRequests.Contains(_dashboardState.Peek()))
                    {
                        Logger.Debug("Disabling " + _dashboardState.Peek());
                        _dashboardStateRequests.Remove(_dashboardState.Pop());
                        Logger.Debug(_dashboardState.Count + " states now, " + _dashboardState.Peek() + " top.");
                    }
                }
                else
                {
                    Logger.Warn("Failed to process disable dashboard state request " + state +
                                ", we don't have that state enabled on any level.");
                }
            }
            OnPropertyChanged("DashboardState");
        }

        public bool DashboardStateContains(DashboardState state)
        {
            lock (_lock)
            {
                return _dashboardState.Contains(state);
            }
        }
    }
}
