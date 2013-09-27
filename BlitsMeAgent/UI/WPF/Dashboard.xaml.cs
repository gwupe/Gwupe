using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using BlitsMe.Agent.Components;
using BlitsMe.Agent.Components.Alert;
using BlitsMe.Agent.Components.Notification;
using BlitsMe.Agent.Components.Person;
using BlitsMe.Agent.Managers;
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
    public partial class Dashboard
    {
#if DEBUG
        private const int ActivityTimeout = 60000;
#else
        private const int ActivityTimeout = 600000;
#endif
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Dashboard));
        private readonly BlitsMeClientAppContext _appContext;

        private LoginControl loginControl;
        // Observable mirror for engagements as engagement windows
        private EngagementWindowList _engagementWindows;
        // Observable mirror for Persons as RosterElements
        private RosterList AllRosterList;
        private CollectionViewSource _notificationView;
        private Timer activateEngagementChecker;
        // Dispatching collection for notifications (to be used by everything)
        internal DispatchingCollection<ObservableCollection<Notification>, Notification> NotificationList { get; set; }
        internal DispatchingCollection<ObservableCollection<Alert>, Alert> AlertList { get; set; }
        private SearchWindow _searchWindow;
        private UserInfoWindow _userInfoWindow;
        private Timer _searchCountDown;
        private readonly Object _searchLock = new Object();
        internal DashboardDataContext DashboardData;
        internal RosterList ActiveRosterList;
        internal RosterList SearchRosterList;
        internal bool Searching;

        public Dashboard(BlitsMeClientAppContext appContext)
        {
            this.InitializeComponent();
            InitializingScreen = true;
            this._appContext = appContext;
            SetupNotificationHandler();
            SetupRoster();
            // Setup the various data contexts and sources
            DashboardData = new DashboardDataContext(this);
            DataContext = DashboardData;
            _appContext.CurrentUserManager.CurrentUserChanged += delegate { SetupCurrentUserListener(); };
            SetupEngagementWindows();
            appContext.LoginManager.LoggedOut += LoginManagerOnLoggedOut;
            activateEngagementChecker = new Timer(30000);
            activateEngagementChecker.Elapsed += CheckActiveEngagements;
            activateEngagementChecker.Start();
            Logger.Info("Dashboard setup completed");
        }

        #region Overlay Screen Management

        internal bool InitializingScreen
        {
            set
            {
                if (Dispatcher.CheckAccess())
                {
                    if (value)
                    {
                        ModalPrompt.ContentTemplate = FindResource("InitializingWindow") as DataTemplate;
                        ModalPrompt.Visibility = Visibility.Visible;
                        Panel.SetZIndex(ModalPrompt, 2);
                    }
                    else
                    {
                        ModalPrompt.Visibility = Visibility.Hidden;
                        Panel.SetZIndex(ModalPrompt, -2);
                    }
                }
                else
                    Dispatcher.Invoke(new Action(() => LoggingInScreen = value));
            }
        }

        internal bool LoggingInScreen
        {
            set
            {
                if (Dispatcher.CheckAccess())
                {
                    if (value)
                    {
                        ModalPrompt.ContentTemplate = FindResource("LoggingInWindow") as DataTemplate;
                        ModalPrompt.Visibility = Visibility.Visible;
                        Panel.SetZIndex(ModalPrompt, 2);
                    }
                    else
                    {
                        ModalPrompt.Visibility = Visibility.Hidden;
                        Panel.SetZIndex(ModalPrompt, -2);
                    }
                }
                else
                    Dispatcher.Invoke(new Action(() => LoggingInScreen = value));
            }
        }

        internal void ShowLoginScreen(bool passwordError = false)
        {
            if (Dispatcher.CheckAccess())
            {
                ModalPrompt.ContentTemplate = null;
                ModalPrompt.Content = loginControl ?? (loginControl = new LoginControl());
                if (passwordError) loginControl.LoginFailed();
                ModalPrompt.Visibility = Visibility.Visible;
                Panel.SetZIndex(ModalPrompt, 2);
            }
            else
                Dispatcher.Invoke(new Action(() => ShowLoginScreen(passwordError)));
        }

        #endregion

        #region Current User interaction

        /// <summary>
        /// Listen for changes on the current user (so that if we change the current info, we know about it)
        /// </summary>
        private void SetupCurrentUserListener()
        {
            Logger.Debug("Setting up new listener for " + _appContext.CurrentUserManager.CurrentUser.Name);
            _appContext.CurrentUserManager.CurrentUser.PropertyChanged += CurrentUserOnPropertyChanged;
            if (Dispatcher.CheckAccess())
            {
                DashboardData.Title = _appContext.CurrentUserManager.CurrentUser.Name;
            }
            else
            {
                Dispatcher.Invoke(
                    new Action(() => { DashboardData.Title = _appContext.CurrentUserManager.CurrentUser.Name; }));
            }
        }

        /// <summary>
        /// Event which gets fired when the current user changes some of his details
        /// </summary>
        /// <param name="sender">Who fired the event</param>
        /// <param name="propertyChangedEventArgs">Info about the property which was changed</param>
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


        private void UserInfo_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_appContext.ConnectionManager.IsOnline())
            {
                if (_userInfoWindow == null)
                {
                    _userInfoWindow = new UserInfoWindow(_appContext);
                }
                // Clear currently engaged
                var currentEngaged = _appContext.RosterManager.CurrentlyEngaged;
                if (currentEngaged != null) currentEngaged.IsCurrentlyEngaged = false;
                // Set main active window
                ActiveContent.Content = _userInfoWindow;
                _userInfoWindow.SetAsMain(this);
            }
        }


        #endregion

        #region Notification Handling

        /// <summary>
        /// Setup the notification handling
        /// </summary>
        private void SetupNotificationHandler()
        {
            // Handle the notifications
            NotificationList =
                new DispatchingCollection<ObservableCollection<Notification>, Notification>(
                    _appContext.NotificationManager.Notifications, Dispatcher);
            _notificationView = new CollectionViewSource { Source = NotificationList };
            _notificationView.Filter += NotificationFilter;
            Notifications.ItemsSource = _notificationView.View;
            AlertList = new DispatchingCollection<ObservableCollection<Alert>, Alert>(_appContext.NotificationManager.Alerts,
                                                                                      Dispatcher);
            Alerts.ItemsSource = AlertList;
            _appContext.NotificationManager.Notifications.CollectionChanged += NotificationsOnCollectionChanged;
        }

        /// <summary>
        /// If the notification collection changes, we must refresh the view.
        /// </summary>
        /// <param name="sender">sender object</param>
        /// <param name="notifyCollectionChangedEventArgs">Collection change information</param>
        private void NotificationsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            Logger.Debug("Notification Collection has changed, refreshing");
            if (_appContext.IsShuttingDown)
                return;
            // Multithreaded handling, make sure only dispatcher updates UI objects
            if (Dispatcher.CheckAccess())
                _notificationView.View.Refresh();
            else
                Dispatcher.Invoke(new Action(() => _notificationView.View.Refresh()));
        }


        /// <summary>
        /// The filter which determines whether a notification should be shown, this is for general notifications, filters out notifications for 
        /// a specific user.
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="eventArgs">filter args</param>
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

        #endregion

        #region Windowing Stuff

        private void WindowStateChanged(object sender, EventArgs e)
        {
            HideIfMinimized(sender, e);
        }

        internal new void Hide()
        {
            if (Dispatcher.CheckAccess())
            {
                base.Hide();
            }
            else
            {
                Dispatcher.Invoke(new Action(Hide));
            }
        }

        internal new void Show()
        {
            if (Dispatcher.CheckAccess())
            {
                base.Show();
                Activate();
                Focus();
                if (_appContext.UIManager.UpdateNotification != null && !_appContext.UIManager.UpdateNotification.IsClosed)
                {
                    _appContext.UIManager.UpdateNotification.Show();
                }

            }
            else
            {
                Dispatcher.Invoke(new Action(Show));
            }
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

        private void SetupRoster()
        {
            var dispatchingCollection = new DispatchingCollection<ObservableCollection<Attendance>, Attendance>(_appContext.RosterManager.ServicePersonAttendanceList, Dispatcher);

            AllRosterList = new MainRosterList(dispatchingCollection, AllContacts);
            AllContacts.LostFocus += Contacts_LostFocus;
            AllContacts.DataContext = AllRosterList.ContactsView;

            ActiveRosterList = new ActiveRosterList(dispatchingCollection, CurrentlyActiveContacts);
            CurrentlyActiveContacts.LostFocus += Contacts_LostFocus;
            CurrentlyActiveContacts.DataContext = ActiveRosterList.ContactsView;
            ActiveRosterList.ContactsView.View.CollectionChanged += ActiveRosterChanged;

            SearchRosterList = new SearchRosterList(dispatchingCollection, SearchContacts, SearchBox);
            SearchContacts.LostFocus += Contacts_LostFocus;
            SearchContacts.DataContext = SearchRosterList.ContactsView;
        }

        /// <summary>
        /// Called when the active roster changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="notifyCollectionChangedEventArgs"></param>
        private void ActiveRosterChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            CurrentlyActiveContactsWindowlet.Visibility = ActiveRosterList.ContactsView.View.Cast<Attendance>().Any() ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// When we click on another button, Team must lose focus so when its clicked on again, the select item event fires.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Contacts_LostFocus(object sender, RoutedEventArgs e)
        {
            AllContacts.SelectedItem = null;
            CurrentlyActiveContacts.SelectedItem = null;
            SearchContacts.SelectedItem = null;
        }

        /// <summary>
        /// When a normal contact is selected, we show the engagement.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ContactSelected(object sender, RoutedEventArgs e)
        {
            ListBoxItem item = e.Source as ListBoxItem;
            if (item != null)
            {
                ShowEngagement(item.DataContext as Attendance);
            }
        }

        #endregion

        #region Engagement Handling

        private void SetupEngagementWindows()
        {
            // Setup the engagementWindow list as a mirror of the engagements
            _engagementWindows = new EngagementWindowList(_appContext, NotificationList, OnEngagementPropertyChange, Dispatcher);
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

        private void OnEngagementPropertyChange(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            var engagement = sender as Engagement;
            if (engagement != null)
            {
                if (propertyChangedEventArgs.PropertyName.Equals("IsUnread"))
                {
                    if (Dispatcher.CheckAccess())
                    {
                        ResetUnreadIfVisible(engagement);
                    }
                    else
                    {
                        Dispatcher.Invoke(new Action(() => ResetUnreadIfVisible(engagement)));
                    }
                }
                else if (propertyChangedEventArgs.PropertyName.Equals("Active"))
                {
                    if (Dispatcher.CheckAccess())
                    {
                        RefreshRosters();
                    }
                    else
                    {
                        Dispatcher.Invoke(new Action(RefreshRosters));
                    }
                }
            }
        }

        private void ResetUnreadIfVisible(Engagement engagement)
        {
            var currentEngagement = ActiveContent.Content as EngagementWindow;
            if (currentEngagement != null && engagement.IsUnread &&
                currentEngagement.Engagement == engagement &&
                currentEngagement.EngagementVisibleContent == EngagementVisibleContent.Chat)
            {
                engagement.IsUnread = false;
            }
        }

        /// <summary>
        /// Called on a timer to check which roster elements are no longer active.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="elapsedEventArgs"></param>
        private void CheckActiveEngagements(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            if (!Dispatcher.CheckAccess())
                Dispatcher.Invoke(new Action(() => CheckActiveEngagements(sender, elapsedEventArgs)));
            else
            {
                if (ActiveRosterList.ContactsView.View.Cast<Attendance>().ToList().Any(attendance => !attendance.IsActive))
                {
                    RefreshRosters();
                }
            }
        }

        /// <summary>
        /// Called if there is new activity on an engagement, so we can prompt the user
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="args">what is the activity</param>
        internal void EngagementManagerOnNewActivity(object sender, EngagementActivity args)
        {
            if (_appContext.IsShuttingDown)
                return;
            Show();
            RefreshRosters();
        }

        private void RefreshRosters()
        {
            if (!Dispatcher.CheckAccess())
                Dispatcher.Invoke(new Action(RefreshRosters));
            else
            {
                Logger.Debug("Refreshing Rosters");
                ActiveRosterList.ContactsView.View.Refresh();
                AllRosterList.ContactsView.View.Refresh();
            }
        }

        private void ShowEngagement(Attendance attendance)
        {
            EngagementWindow egw = _engagementWindows.GetEngagementWindow(attendance);
            if (egw != null)
            {
                attendance.IsCurrentlyEngaged = true;
                ActiveContent.Content = egw;
                egw.SetAsMain(this);
                egw.ShowChat();
            }
            else
            {
                Logger.Error("Failed to find an engagement window for " + attendance.Person);
            }
        }

        #endregion

        #region Search Handling

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            // This gets called on init (where appContext is null), but we don't want to search that anyway
            if (_appContext != null)
            {
                if (!String.IsNullOrWhiteSpace(SearchBox.Text))
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
            Searching = true;
            DashboardData.SearchContactsVisibility = 0;
            if (_searchWindow == null)
            {
                _searchWindow = new SearchWindow(_appContext);
                _searchCountDown = new Timer(500) { AutoReset = false };
                _searchCountDown.Elapsed += ProcessSearch;
            }
            ActiveContent.Content = _searchWindow;
            _searchWindow.SetAsMain(this);
        }

        private void SearchBox_LostFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            Searching = false;
            DashboardData.SearchContactsVisibility = 0;
        }

        private void ProcessSearch(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            Logger.Debug("Processing Search Request");
            lock (_searchLock)
            {
                // once again, just to notify
                if (Dispatcher.CheckAccess())
                {
                    SearchRosterList.ContactsView.View.Refresh();
                    DashboardData.SearchContactsVisibility = 0;
                }
                else
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        SearchRosterList.ContactsView.View.Refresh();
                        DashboardData.SearchContactsVisibility = 0;
                    }));
                }
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
                    if (!String.IsNullOrWhiteSpace(searchQuery))
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

        #endregion

        #region UI Mechanics

        /// <summary>
        /// Event called when we are logged out
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="loginEventArgs"></param>
        private void LoginManagerOnLoggedOut(object sender, LoginEventArgs loginEventArgs)
        {
            Reset();
        }

        /// <summary>
        /// Event called by Window when logout button is hit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Logout(object sender, EventArgs e)
        {
            _appContext.LoginManager.Logout();
        }

        /// <summary>
        /// Event called when the exit button is pressed, starts a new thread to shutdown the application (this is so the dispatcher is not 
        /// responsible for this)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitApplication(object sender, RoutedEventArgs e)
        {
            Thread shutdownThread = new Thread(_appContext.Shutdown) { IsBackground = true, Name = "shutdownByUserThread" };
            shutdownThread.Start();
        }

        public new void Close()
        {
            if (Dispatcher.CheckAccess())
            {
                Dispatcher.InvokeShutdown();
                if (activateEngagementChecker != null)
                    activateEngagementChecker.Stop();
                base.Close();
            }
            else
            {
                Dispatcher.Invoke(new Action(Close));
            }
        }


        /// <summary>
        /// Reset the dashboard (normally on disconnect)
        /// </summary>
        internal void Reset()
        {
            if (!Dispatcher.CheckAccess())
                Dispatcher.Invoke(new Action(Reset));
            else
            {
                ActiveContent.Content = null;
                _searchWindow = null;
            }
        }

        /// <summary>
        /// Setup so that we can receive messages from other windows in the OS
        /// </summary>
        /// <param name="e"></param>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(_appContext.WndProc);
        }

        #endregion

        /// <summary>
        /// Called when the settings button is pushed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Settings(object sender, System.Windows.RoutedEventArgs e)
        {
            // TODO: Add event handler implementation here.
        }

    }

    internal class DashboardDataContext : INotifyPropertyChanged
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(DashboardDataContext));

        private readonly Dashboard _dashboard;
        private string _customTitle;

        public DashboardDataContext(Dashboard dashboard)
        {
            _dashboard = dashboard;
        }

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

        public Visibility SearchContactsVisibility
        {
            set { OnPropertyChanged("SearchContactsVisibility"); }
            get
            {
                Logger.Debug("Reading search results");
                return (_dashboard.Searching && _dashboard.SearchRosterList != null && _dashboard.SearchRosterList.ContactsView.View.Cast<object>().Any())
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }
    }

}