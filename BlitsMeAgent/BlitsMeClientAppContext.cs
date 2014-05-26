using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using BlitsMe.Agent.Components;
using BlitsMe.Agent.Components.Schedule;
using BlitsMe.Agent.Managers;
using BlitsMe.Agent.Misc;
using BlitsMe.Agent.UI;
using BlitsMe.Agent.UI.WPF;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using BlitsMe.Common;
using BlitsMe.Common.Security;
using BlitsMe.ServiceProxy;
using log4net;
using log4net.Config;
using log4net.Core;
using log4net.Repository.Hierarchy;

namespace BlitsMe.Agent
{
    public enum BlitsMeOption
    {
        Minimize
    };

    public class BlitsMeClientAppContext : ApplicationContext
    {
        internal List<BlitsMeOption> Options { get; private set; }
        private static readonly ILog Logger = LogManager.GetLogger(typeof(BlitsMeClientAppContext));
        internal readonly BlitsMeServiceProxy BlitsMeServiceProxy;
        private readonly SystemTray _systray;
        //public Dashboard UIDashBoard;
        internal P2PManager P2PManager;
        private RequestManager _requestManager;
        internal CurrentUserManager CurrentUserManager { get; private set; }
        internal RosterManager RosterManager { get; private set; }
        internal LoginManager LoginManager { get; private set; }
        internal ConnectionManager ConnectionManager { get; private set; }
        internal EngagementManager EngagementManager { get; private set; }
        internal NotificationManager NotificationManager { get; private set; }
        internal SearchManager SearchManager { get; private set; }
        internal UIManager UIManager { get; private set; }
        internal RepeaterManager RepeaterManager { get; private set; }
        internal SettingsManager SettingsManager { get; private set; }
        //internal Thread DashboardUiThread;
        internal bool IsShuttingDown { get; private set; }
        internal readonly BLMRegistry Reg = new BLMRegistry();
        internal readonly String StartupVersion;
        internal readonly ScheduleManager ScheduleManager;
        private IdleState _idleState;
        internal ObservableCollection<String> ChangeLog = new ObservableCollection<string>();
        internal string ChangeDescription { get; set; }

        internal static BlitsMeClientAppContext CurrentAppContext;

        /// <summary>
        /// This class should be created and passed into Application.Run( ... )
        /// </summary>
        /// <param name="options"> </param>
        public BlitsMeClientAppContext(List<BlitsMeOption> options)
        {
            CurrentAppContext = this;
            Options = options;
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
            ConnectionManager = new ConnectionManager();
            LoginManager = new LoginManager();
            P2PManager = new P2PManager();
            RosterManager = new RosterManager();
            EngagementManager = new EngagementManager();
            NotificationManager = new NotificationManager();
            SearchManager = new SearchManager();
            CurrentUserManager = new CurrentUserManager();
            SettingsManager = new SettingsManager();
            _systray = new SystemTray();
            UIManager = new UIManager();
            _requestManager = new RequestManager();
            ScheduleManager = new ScheduleManager();
            ScheduleManager.AddTask(new CheckUpgradeTask(this) { PeriodSeconds = 120 });
            ScheduleManager.AddTask(new CheckServiceTask(this) { PeriodSeconds = 120 });
            ScheduleManager.AddTask(new DetectIdleTask(this));
            RepeaterManager = new RepeaterManager();
            SetupChangeLog();
            // Start all the Active Managers
            UIManager.Start();
            ScheduleManager.Start();
            ConnectionManager.Start();
            LoginManager.Start();
            _systray.Start();
            // Set correct last version
            Reg.LastVersion = StartupVersion;
        }

        private void SetupChangeLog()
        {
            if (!StartupVersion.Equals(Reg.LastVersion))
            {
                Logger.Debug("BlitsMe has been upgraded, reading and displaying changelog");
                try
                {
                    using (
                        Stream stream =
                            Assembly.GetExecutingAssembly().GetManifestResourceStream("BlitsMe.Agent.changelog.txt"))
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            if (reader.Peek() >= 0)
                            {
                                ChangeDescription = reader.ReadLine();
                            }
                            while (reader.Peek() >= 0)
                            {
                                ChangeLog.Add(reader.ReadLine());
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to read changelog file : " + e.Message);
                }
            }
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
            UIManager.Show();
        }
        /*
        private void RunDashboard()
        {
            UIDashBoard = new Dashboard(this);
            _dashboardReady.Set();
            Dispatcher.Run();
        }*/

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

        /*
        internal void SetupAndRunDashboard()
        {
            if (DashboardUiThread == null)
            {
                DashboardUiThread = new Thread(RunDashboard) { Name = "dashboardUIThread" };
                DashboardUiThread.SetApartmentState(ApartmentState.STA);
                DashboardUiThread.Start();
                _dashboardReady.WaitOne();
            }
        }*/

        // Handle messages from the dashboard window
        public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == OsUtils.WM_SHOWBM &&
#if DEBUG
                wParam.ToInt32() == 0
#else
                wParam.ToInt32() == 1
#endif
                )
            {
                Logger.Debug("Received show message to my handle " + hwnd);
                UIManager.Show();
                handled = true;
            }
            else if (msg == OsUtils.WM_SHUTDOWNBM &&
#if DEBUG
                wParam.ToInt32() == 0
#else
                wParam.ToInt32() == 1
#endif
            )
            {
                Logger.Debug("Received shutdown message to my handle " + hwnd);
                Thread shutdownThread = new Thread(Shutdown) { IsBackground = true, Name = "shutdownByMessageThread" };
                shutdownThread.Start();
            }
            else if (msg == OsUtils.WM_UPGRADEBM &&
#if DEBUG
                wParam.ToInt32() == 0
#else
                wParam.ToInt32() == 1
#endif
            )
            {
                Logger.Debug("Received upgrade message to my handle " + hwnd);
                Thread upgradeThread = new Thread(new CheckUpgradeTask(this).RunTask) { IsBackground = true, Name = "upgradeByMessageThread" };
                upgradeThread.Start();
            }
            return IntPtr.Zero;
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
            // Stop scheduled things from happening
            if (ScheduleManager != null)
                ScheduleManager.Close();
            // Logout and prevent trying to log back in
            if (LoginManager != null)
                LoginManager.Close();
            // Close up the engagements
            if (EngagementManager != null)
                EngagementManager.Close();
            // No more notifications
            if (NotificationManager != null)
                NotificationManager.Close();
            // Clear and close contact list
            if (RosterManager != null)
                RosterManager.Close();
            // No more searching
            if (SearchManager != null)
                SearchManager.Close();
            // Don't need service contact anymore
            if (BlitsMeServiceProxy != null)
                BlitsMeServiceProxy.close();
            // Connection can close
            if (ConnectionManager != null)
                ConnectionManager.Close();
            // Stop showing stuff
            if (UIManager != null)
                UIManager.Close();
            // Remove SystemTray
            if (_systray != null)
                _systray.Close();
            // Done
            Logger.Info("BlitsMe.Agent has shut down");
            base.ExitThreadCore();
        }
    }
}