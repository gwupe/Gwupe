using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using BlitsMe.Agent.Components;
using BlitsMe.Agent.Managers;
using BlitsMe.Agent.Misc;
using BlitsMe.Agent.UI;
using BlitsMe.Agent.UI.WPF;
using BlitsMe.Cloud.Messaging.API;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using BlitsMe.Common.Security;
using BlitsMe.ServiceProxy;
using log4net;
using log4net.Config;
using Timer = System.Timers.Timer;
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
        internal Thread _dashboardUIThread;
        internal bool isShuttingDown { get; private set; }
        private readonly BLMRegistry _reg = new BLMRegistry();
        private readonly Timer _upgradeCheckTimer;
        private readonly String _startupVersion;

        /// <summary>
        /// This class should be created and passed into Application.Run( ... )
        /// </summary>
        public BlitsMeClientAppContext()
        {
            XmlConfigurator.Configure(Assembly.GetExecutingAssembly().GetManifestResourceStream("BlitsMe.Agent.log4net.xml"));
            _startupVersion = Regex.Replace(FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion, "\\.[0-9]+$", "");
            Logger.Info("BlitsMe" + Program.BuildMarker + ".Agent Starting up [" + _startupVersion + "]");
#if DEBUG
            foreach (var manifestResourceName in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                Logger.Debug("Embedded Resource : " + manifestResourceName);
            }
#endif
            BlitsMeServiceProxy = new BlitsMeServiceProxy();
            ConnectionManager = new ConnectionManager(this);
            LoginManager = new LoginManager(this);
            _requestManager = new RequestManager(this);
            P2PManager = new P2PManager();
            RosterManager = new RosterManager(this);
            EngagementManager = new EngagementManager(this);
            NotificationManager = new NotificationManager(this);
            SearchManager = new SearchManager(this);
            CurrentUserManager = new CurrentUserManager(this);
            EngagementManager.NewActivity += OnNewEngagementActivity;
            _systray = new SystemTray(this);
            _upgradeCheckTimer = new Timer(60000);
            _upgradeCheckTimer.Elapsed += UpgradeCheckTimerOnElapsed;
            _upgradeCheckTimer.Enabled = true;
        }

        private void UpgradeCheckTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            try
            {
                var regVersion = _reg.getRegValue("Version", true);
                Logger.Debug("Checking for agent upgrade " + _startupVersion + " vs " + regVersion);
                if (new Version(regVersion).CompareTo(new Version(_startupVersion)) != 0)
                {
                    Logger.Info("My file version has changed " + _startupVersion + " => " + regVersion +
                                ", closing to re-open as new version.");
                    try
                    {
                        Process.Start(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) +
                                      "\\BlitsMe.Agent.Upgrade.exe");
                    } catch(Exception e)
                    {
                        Logger.Error("Failed to start the upgrade exe, but will stop myself anyway.");
                    }
                    Shutdown();
                }
            } catch(Exception e)
            {
                Logger.Error("Failed to check version and act : " + e.Message,e);
            }
        }

        public String Version
        {
            get
            {
                var fullVersion = new Version(FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion);
                return fullVersion.Major + "." + fullVersion.Minor;
            }
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
                    UIDashBoard.Dispatcher.Invoke(new Action(() =>
                                                                {
                                                                    UIDashBoard.EngagementManagerOnNewActivity(sender,
                                                                                                              args);
                                                                }));
                }
            }
        }

        public BlitsMeServiceProxy BlitsMeService
        {
            get { return BlitsMeServiceProxy; }
        }

        public PingRs ping(PingRq request)
        {
            return new PingRs();
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
            if(UIDashBoard != null)
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
            ElevateApprovalWindow approvalWindow = new ElevateApprovalWindow();
            approvalWindow.Owner = parentWindow;
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
            if (_dashboardUIThread == null)
            {
                _dashboardUIThread = new Thread(RunDashboard) { Name = "dashboardUIThread" };
                _dashboardUIThread.SetApartmentState(ApartmentState.STA);
                _dashboardUIThread.Start();
                while (UIDashBoard == null || !UIDashBoard.IsInitialized)
                {
                    Thread.Sleep(50);
                }
            }
        }

        public void Shutdown()
        {
            ExitThread();
        }

        // On exit
        protected override void ExitThreadCore()
        {
            this.isShuttingDown = true;
            // before we exit, lets cleanup
            if (_dashboardUIThread != null)
            {
                UIDashBoard.Dispatcher.InvokeShutdown();
                _dashboardUIThread.Abort();
            }
            if (BlitsMeServiceProxy != null)
                BlitsMeServiceProxy.close();
            if (LoginManager != null)
                LoginManager.Close();
            if (ConnectionManager != null)
                ConnectionManager.Close();
            if (_systray != null)
                _systray.close();
            if (RosterManager != null)
                RosterManager.Close();
            if (EngagementManager != null)
                EngagementManager.Close();
            if(NotificationManager != null)
                NotificationManager.Close();
            if (SearchManager != null)
                SearchManager.Close();
            // Done
            Logger.Info("BlitsMe.Agent has shut down");
            base.ExitThreadCore();
        }
    }
}