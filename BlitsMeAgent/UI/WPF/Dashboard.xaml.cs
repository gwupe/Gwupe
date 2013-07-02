using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using BlitsMe.Agent.Components;
using BlitsMe.Agent.Components.Alert;
using BlitsMe.Agent.Components.Notification;
using BlitsMe.Agent.Components.Person;
using BlitsMe.Agent.UI.WPF.Engage;
using BlitsMe.Agent.UI.WPF.Roster;
using BlitsMe.Agent.UI.WPF.Search;
using log4net;
using Timer = System.Timers.Timer;

namespace BlitsMe.Agent.UI.WPF
{
    /// <summary>
    /// Interaction logic for Dashboard.xaml
    /// </summary>
    public partial class Dashboard : Window
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Dashboard));
        private readonly BlitsMeClientAppContext _appContext;

        // Observable mirror for engagements as engagement windows
        private EngagementWindowList _engagementWindows;
        // Observable mirror for Persons as RosterElements
        private RosterList _rosterList;
        private CollectionViewSource _notificationView;
        // Dispatching collection for notifications (to be used by everything)
        internal DispatchingCollection<ObservableCollection<Notification>, Notification> NotificationList { get; set; }
        internal DispatchingCollection<ObservableCollection<Alert>, Alert> AlertList { get; set; } 
        private SearchWindow _searchWindow;
        private UserInfoWindow _userInfoWindow;
        private Timer _searchCountDown;
        private readonly Object _searchLock = new Object();
        internal DashboardDataContext DashboardData;

        public Dashboard(BlitsMeClientAppContext appContext)
        {
            this.InitializeComponent();
            this._appContext = appContext;
            SetupNotificationHandler();
            SetupRoster();
            // Setup the various data contexts and sources
            DashboardData = new DashboardDataContext();
            DataContext = DashboardData;
            _appContext.CurrentUserManager.CurrentUserChanged += delegate { SetupCurrentUserListener(); };
            SetupEngagementWindows();
            IsEnabledChanged += OnIsEnabledChanged;
            Logger.Info("Dashboard setup completed");
        }

        private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if(this.IsEnabled)
            {
                Panel.SetZIndex(DisableScreen,-1);
            } else
            {
                Panel.SetZIndex(DisableScreen,1);
            }
        }

        private void SetupEngagementWindows()
        {
// Setup the engagementWindow list as a mirror of the engagements
            _engagementWindows = new EngagementWindowList(_appContext, NotificationList, Dispatcher);
            try
            {
                _engagementWindows.SetList(_appContext.EngagementManager.Engagements, "SecondPartyUsername");
            }
            catch (Exception e)
            {
                Logger.Error("Failed to set the list : " + e.Message, e);
            }
            _appContext.EngagementManager.NewActivity += EngagementManagerOnNewActivity;
        }

        private void SetupRoster()
        {
            _rosterList = new RosterList(_appContext, Dispatcher);
            _rosterList.SetList(_appContext.RosterManager.ServicePersonList, "Username");
            Team.LostFocus += Team_LostFocus;
            Team.DataContext = _rosterList.RosterViewSource;
        }

        private void SetupNotificationHandler()
        {
            // Handle the notifications
            NotificationList =
                new DispatchingCollection<ObservableCollection<Notification>, Notification>(
                    _appContext.NotificationManager.Notifications, Dispatcher);
            _notificationView = new CollectionViewSource {Source = NotificationList};
            _notificationView.Filter += NotificationFilter;
            Notifications.ItemsSource = _notificationView.View;
            AlertList = new DispatchingCollection<ObservableCollection<Alert>, Alert>(_appContext.NotificationManager.Alerts,
                                                                                      Dispatcher);
            Alerts.ItemsSource = AlertList;
            _appContext.NotificationManager.Notifications.CollectionChanged += NotificationsOnCollectionChanged;
        }

        internal void Reset()
        {
            if (!Dispatcher.CheckAccess())
                Dispatcher.Invoke(new Action(Reset));
            else
            {
                ActiveContent.Content = null;
                SearchBox.Text = "Search";
                _searchWindow = null;
            }
        }

        private void SetupCurrentUserListener()
        {
            Logger.Debug("Setting up new listener for " + _appContext.CurrentUserManager.CurrentUser.Name);
            _appContext.CurrentUserManager.CurrentUser.PropertyChanged += CurrentUserOnPropertyChanged;
            if (Dispatcher.CheckAccess())
            {
                DashboardData.Title = _appContext.CurrentUserManager.CurrentUser.Name;
            } else
            {
                Dispatcher.Invoke(
                    new Action(() => { DashboardData.Title = _appContext.CurrentUserManager.CurrentUser.Name; }));
            }
        }

        private void CurrentUserOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (_appContext.IsShuttingDown)
                return;
            if (Dispatcher.CheckAccess())
            {
                if (propertyChangedEventArgs.PropertyName.Equals("Name"))
                {
                    Logger.Debug("Name has changed : " + _appContext.CurrentUserManager.CurrentUser.Name);
                    DashboardData.Title = _appContext.CurrentUserManager.CurrentUser.Name;
                }
            }
            else
                Dispatcher.Invoke(new Action(() => CurrentUserOnPropertyChanged(sender, propertyChangedEventArgs)));
        }

        private void NotificationsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            if (_appContext.IsShuttingDown)
                return;
            // Multithreaded handling, make sure only dispatcher updates UI objects
            if (Dispatcher.CheckAccess())
                _notificationView.View.Refresh();
            else
                Dispatcher.Invoke(new Action(() => _notificationView.View.Refresh()));
        }

        internal void EngagementManagerOnNewActivity(object sender, EngagementActivityArgs args)
        {
            if (_appContext.IsShuttingDown)
                return;
            if (Dispatcher.CheckAccess())
            {
                ShowEngagement(args.Engagement.SecondParty);
            }
            else
            {
                Dispatcher.Invoke(new Action(() => ShowEngagement(args.Engagement.SecondParty)));
            }
        }

        private void NotificationFilter(object sender, FilterEventArgs eventArgs)
        {
            if (_appContext.IsShuttingDown)
                return;
            Notification notification = eventArgs.Item as Notification;
            if (notification != null && (notification.AssociatedUsername == null || notification.AssociatedUsername.Equals("")))
            {
                eventArgs.Accepted = true;
            }
            else
            {
                eventArgs.Accepted = false;
            }
        }

        public void Logout(object sender, EventArgs e)
        {
            _appContext.LoginManager.Logout(true);
        }

        #region Windowing Stuff

        private void WindowStateChanged(object sender, EventArgs e)
        {
            HideIfMinimized(sender, e);
        }

        private void HideIfMinimized(object sender, EventArgs e)
        {
            if (WindowState.Minimized == this.WindowState)
            {
                // Don't hide if minimized
               // this.Hide();
            }
        }

        private void HideIfClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        #endregion

        #region Roster Handling

        // When we click on another button, Team must lose focus so when its clicked on again, the select item event fires.
        private void Team_LostFocus(object sender, RoutedEventArgs e)
        {
            Team.SelectedItem = null;
        }

        private void TeamMemberSelected(object sender, RoutedEventArgs e)
        {
            ListBoxItem item = e.Source as ListBoxItem;
            RosterElement rosterElement = item.Content as RosterElement;
            ShowEngagement(rosterElement.Person);
        }

        private void RemoveTeamMember(object sender, RoutedEventArgs e)
        {
            Logger.Debug("Removing Team Member " + ((ListBoxItem)sender));
        }

        private void ShowEngagement(Person person)
        {
            EngagementWindow egw = _engagementWindows.GetEngagementWindow(person);
            if (egw != null)
            {
                ActiveContent.Content = egw;
                egw.SetAsMain(this);
                try
                {
                    ClearActiveRosterElement();
                    RosterElement element = _rosterList.GetElement(person.Username);
                    element.IsActive = true;
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to select the roster item for " + person.Username);
                }
            }
            else
            {
                Logger.Error("Failed to find an engagement window for peron " + person);
            }
        }

        private void ClearActiveRosterElement()
        {
            foreach (var rosterElement in _rosterList)
            {
                rosterElement.IsActive = false;
            }
        }

        private void Search_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // This gets called on init (where appContext is null), but we don't want to search that anyway
            if (_appContext != null)
            {
                if (!SearchBox.Text.Equals("") && !SearchBox.Text.Equals("Search"))
                {
                    lock (_searchLock)
                    {
                        // If its already enabled, reset it
                        if (_searchCountDown.Enabled)
                        {
                            // Reset the timer
                            _searchCountDown.Stop();
                        }
                        _searchCountDown.Start();
                    }
                }
            }
        }

        private void SearchBox_GotFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            if ("Search".Equals(SearchBox.Text))
            {
                SearchBox.Text = "";
            }
            if (_searchWindow == null)
            {
                _searchWindow = new SearchWindow(_appContext);
                _searchCountDown = new Timer(500) { AutoReset = false };
                _searchCountDown.Elapsed += ProcessSearch;
            }
            ActiveContent.Content = _searchWindow;
            _searchWindow.SetAsMain(this);
            ClearActiveRosterElement();
        }

        private void ProcessSearch(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            lock (_searchLock)
            {
                try
                {
                    String searchQuery = "";
                    if (Dispatcher.CheckAccess())
                    {
                        searchQuery = SearchBox.Text;
                    }
                    else
                    {
                        Dispatcher.Invoke(new Action(delegate { searchQuery = SearchBox.Text; }));
                    }
                    if (!searchQuery.Equals("") && !searchQuery.Equals("Search"))
                    {
                        _appContext.SearchManager.Search(searchQuery);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to perform search : " + e.Message, e);
                }
            }
        }

        private void SearchBox_LostFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            if ("".Equals(SearchBox.Text))
            {
                SearchBox.Text = "Search";
            }
        }

        private void UserInfo_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_appContext.ConnectionManager.IsOnline())
            {
                if (_userInfoWindow == null)
                {
                    _userInfoWindow = new UserInfoWindow(_appContext);
                }
                ActiveContent.Content = _userInfoWindow;
                _userInfoWindow.SetAsMain(this);
                ClearActiveRosterElement();
            }
        }

        #endregion

        private void ExitApplication(object sender, RoutedEventArgs e)
        {
            Thread shutdownThread = new Thread(_appContext.Shutdown) { IsBackground = true };
            shutdownThread.Start();
        }
    }

    internal class DashboardDataContext : INotifyPropertyChanged
    {
        private string _customTitle;
        public String Title
        {
            get { return "BlitsMe" + Program.BuildMarker + " - " + _customTitle; }
            set { _customTitle = value; OnPropertyChanged("Title"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}