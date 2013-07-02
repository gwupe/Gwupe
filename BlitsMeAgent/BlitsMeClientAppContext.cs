using System;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using BlitsMe.Agent.Components;
using BlitsMe.Agent.Components.Person.Presence;
using BlitsMe.Agent.Components.Schedule;
using BlitsMe.Agent.Managers;
using BlitsMe.Agent.Misc;
using BlitsMe.Agent.UI;
using BlitsMe.Agent.UI.WPF;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using BlitsMe.Common.Security;
using BlitsMe.ServiceProxy;
using log4net;
using log4net.Config;
using log4net.Core;
using log4net.Repository.Hierarchy;
using Dashboard = BlitsMe.Agent.UI.WPF.Dashboard;

namespace BlitsMe.Agent
{
    public class BlitsMeClientAppContext : ApplicationContext
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(BlitsMeClientAppContext));
        internal readonly BlitsMeServiceProxy BlitsMeServiceProxy;
        private readonly SystemTray _systray;
        public Dashboard UIDashBoard;
        public P2PManager P2PManager;
        private RequestManager _requestManager;
        internal CurrentUserManager CurrentUserManager { get; private set; }
        internal RosterManager RosterManager { get; private set; }
        internal LoginManager LoginManager { get; private set; }
        internal ConnectionManager ConnectionManager { get; private set; }
        internal EngagementManager EngagementManager { get; private set; }
        internal NotificationManager NotificationManager { get; private set; }
        internal SearchManager SearchManager { get; private set; }
        internal Thread DashboardUiThread;
        internal bool IsShuttingDown { get; private set; }
        internal readonly BLMRegistry Reg = new BLMRegistry();
        internal readonly String StartupVersion;
        internal readonly ScheduleManager ScheduleManager;
        private IdleState _idleState;

        /// <summary>
        /// This class should be created and passed into Application.Run( ... )
        /// </summary>
        public BlitsMeClientAppContext()
        {
            XmlConfigurator.Configure(Assembly.GetExecutingAssembly().GetManifestResourceStream("BlitsMe.Agent.log4net.xml"));
            StartupVersion = Regex.Replace(FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion, "\\.[0-9]+$", "");
            Logger.Info("BlitsMe" + Program.BuildMarker + ".Agent Starting up [" + StartupVersion + "]");
#if DEBUG
            foreach (var manifestResourceName in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                Logger.Debug("Embedded Resource : " + manifestResourceName);
            }
#endif
            BlitsMeServiceProxy = new BlitsMeServiceProxy();
            ConnectionManager = new ConnectionManager(this);
            LoginManager = new LoginManager(this);
            P2PManager = new P2PManager();
            RosterManager = new RosterManager(this);
            EngagementManager = new EngagementManager(this);
            NotificationManager = new NotificationManager(this);
            SearchManager = new SearchManager(this);
            CurrentUserManager = new CurrentUserManager(this);
            EngagementManager.NewActivity += OnNewEngagementActivity;
            _systray = new SystemTray(this);
            ConnectionManager.Start();
            _requestManager = new RequestManager(this);
            ScheduleManager = new ScheduleManager(this);
            ScheduleManager.AddTask(new CheckUpgradeTask(this) { PeriodSeconds = 120 });
            ScheduleManager.AddTask(new CheckServiceTask(this) { PeriodSeconds = 120 });
            ScheduleManager.AddTask(new DetectIdleTask(this));
            ScheduleManager.Start();
            // Annoying how long it takes to show the window, so load it here
            SetupAndRunDashboard();
        }

        internal bool Debug
        {
            set { ((Logger) Logger.Logger).Parent.Level = value ? Level.Debug : Level.Info; }
        }

        public String Version(int level = 1)
        {
            var fullVersion = new Version(FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion);
            return fullVersion.Major +
                   (level > 0
                        ? "." + fullVersion.Minor +
                          (level > 1
                               ? "." + fullVersion.Build +
                                 (level > 2 ? "." + fullVersion.Revision : "")
                               : "")
                        : "");
        }

        private void OnNewEngagementActivity(object sender, EngagementActivityArgs args)
        {
            bool runEventManually = (UIDashBoard == null);
            ShowDashboard();
            // If we started up the dashboard with this event, it won't obviously have received it, so lets manually send it
            if (runEventManually)
            {
                Logger.Debug("First startup, running event manually");
                if (UIDashBoard.Dispatcher.CheckAccess())
                {
                    UIDashBoard.EngagementManagerOnNewActivity(sender, args);
                }
                else
                {
                    UIDashBoard.Dispatcher.Invoke(new Action(() => UIDashBoard.EngagementManagerOnNewActivity(sender, args)));
                }
            }
        }

        public BlitsMeServiceProxy BlitsMeService
        {
            get { return BlitsMeServiceProxy; }
        }

        public event EventHandler IdleChanged;

        public void OnIdleChanged(EventArgs e)
        {
            EventHandler handler = IdleChanged;
            if (handler != null) handler(this, e);
        }

        public IdleState IdleState
        {
            get { return _idleState; }
            set { _idleState = value; OnIdleChanged(EventArgs.Empty); }
        }

        public void OnIconClickLaunchDashboard(object sender, EventArgs e)
        {
            if (ConnectionManager.Connection.isEstablished())
            {
                if (LoginManager.IsLoggedIn)
                {
                    ShowDashboard();
                }
                else
                {
                    LoginManager.ShowLoginWindow();
                }
            }
        }

        internal void HideDashboard()
        {
            if (UIDashBoard != null)
            {
                if (UIDashBoard.Dispatcher.CheckAccess())
                    UIDashBoard.Hide();
                else
                    UIDashBoard.Dispatcher.Invoke(new Action(() => UIDashBoard.Hide()));
            }
        }

        internal void ShowDashboard()
        {
            if (LoginManager.IsLoggedIn)
            {
                SetupAndRunDashboard();
                if (UIDashBoard.Dispatcher.CheckAccess())
                {
                    UIDashBoard.Show();
                    UIDashBoard.Activate();
                    UIDashBoard.Topmost = true;
                    UIDashBoard.Topmost = false;
                    UIDashBoard.Focus();
                }
                else
                {
                    UIDashBoard.Dispatcher.Invoke(new Action(() =>
                        {
                            UIDashBoard.Show();
                            UIDashBoard.Activate();
                            UIDashBoard.Topmost = true;
                            UIDashBoard.Topmost = false;
                            UIDashBoard.Focus();
                        }));
                }
            }
        }

        private void RunDashboard()
        {
            UIDashBoard = new Dashboard(this);
            Dispatcher.Run();
        }

        public bool Elevate(Window parentWindow, out String tokenId, out String securityKey)
        {
            ElevateTokenRq erq = new ElevateTokenRq();
            ElevateTokenRs ers = ConnectionManager.Connection.Request<ElevateTokenRq, ElevateTokenRs>(erq);
            ElevateApprovalWindow approvalWindow = new ElevateApprovalWindow {Owner = parentWindow};
            parentWindow.IsEnabled = false;
            approvalWindow.ShowDialog();
            parentWindow.IsEnabled = true;
            if (!approvalWindow.Cancelled)
            {
                tokenId = ers.tokenId;
                securityKey = Util.getSingleton().hashPassword(approvalWindow.ConfirmPassword.Password, ers.token);
                if (approvalWindow.Dispatcher.CheckAccess())
                    approvalWindow.ConfirmPassword.Password = "";
                else
                    approvalWindow.Dispatcher.Invoke(new Action(() => approvalWindow.ConfirmPassword.Password = ""));
            }
            else
            {
                tokenId = null;
                securityKey = null;
                return false;
            }
            return true;
        }

        internal void SetupAndRunDashboard()
        {
            if (DashboardUiThread == null)
            {
                DashboardUiThread = new Thread(RunDashboard) { Name = "dashboardUIThread" };
                DashboardUiThread.SetApartmentState(ApartmentState.STA);
                DashboardUiThread.Start();
                /*while (UIDashBoard == null || !UIDashBoard.IsInitialized)
                {
                    Thread.Sleep(50);
                }*/
            }
        }

        public void Shutdown()
        {
            ExitThread();
        }

        // On exit
        protected override void ExitThreadCore()
        {
            this.IsShuttingDown = true;
            // before we exit, lets cleanup
            if (EngagementManager != null)
                EngagementManager.Close();
            if (NotificationManager != null)
                NotificationManager.Close();
            if (RosterManager != null)
                RosterManager.Close();
            if (DashboardUiThread != null)
            {
                UIDashBoard.Dispatcher.InvokeShutdown();
                DashboardUiThread.Abort();
                DashboardUiThread = null;
            }
            if (SearchManager != null)
                SearchManager.Close();
            if (BlitsMeServiceProxy != null)
                BlitsMeServiceProxy.close();
            if (LoginManager != null)
                LoginManager.Close();
            if (ConnectionManager != null)
                ConnectionManager.Close();
            if (_systray != null)
                _systray.close();
            // Done
            Logger.Info("BlitsMe.Agent has shut down");
            base.ExitThreadCore();
        }
    }
}