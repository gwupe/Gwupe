using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Threading;
using BlitsMe.Agent.Components;
using BlitsMe.Agent.Managers;
using BlitsMe.Agent.UI;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using BlitsMe.ServiceProxy;
using log4net;
using log4net.Config;
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
        internal RosterManager RosterManager { get; private set; }
        internal LoginManager LoginManager { get; private set; }
        internal ConnectionManager ConnectionManager { get; private set; }
        internal EngagementManager EngagementManager { get; private set; }
        internal NotificationManager NotificationManager { get; private set; }
        internal SearchManager SearchManager { get; private set; }
        internal Thread _dashboardUIThread;
        internal bool isShuttingDown { get; private set; }

        /// <summary>
        /// This class should be created and passed into Application.Run( ... )
        /// </summary>
        public BlitsMeClientAppContext()
        {
            XmlConfigurator.Configure(Assembly.GetExecutingAssembly().GetManifestResourceStream("BlitsMe.Agent.log4net.xml"));
            Logger.Info("BlitsMe.Agent Starting up");
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
            EngagementManager.NewActivity += OnNewEngagementActivity;
            _systray = new SystemTray(this);
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