using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Gwupe.Agent.Components;
using Gwupe.Agent.Components.Functions.Chat;
using Gwupe.Agent.Components.Search;
using Gwupe.Agent.UI;
using Gwupe.Agent.UI.WPF;
using Gwupe.Agent.UI.WPF.Engage;
using log4net;

namespace Gwupe.Agent.Managers
{
    internal class UIManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(UIManager));
        private Thread uiManagerThread;
        private Thread uiThread;
        private AutoResetEvent uiReady;
        private Dashboard dashBoard;
        internal UpdateNotification UpdateNotification;
        internal Dashboard Dashboard { get { return dashBoard; } }
        internal bool IsClosed { get; private set; }
        //private Engagement _engagement;
        //private Engagement _remoteEngagement;
        //private Function _chat;
        //private EngagementWindow _engagementWindow;
        private readonly SystemTray _systray;
        //private int _contactsRating;
        
        internal UIManager()
        {
            uiReady = new AutoResetEvent(false);
            GwupeClientAppContext.CurrentAppContext.LoginManager.LoggedIn += LoginManagerOnLoggedIn;
            GwupeClientAppContext.CurrentAppContext.LoginManager.LoggedOut += LoginManagerOnLoggedOut;
            GwupeClientAppContext.CurrentAppContext.LoginManager.LoggingIn += LoginManagerOnLoggingIn;
            GwupeClientAppContext.CurrentAppContext.LoginManager.LoginFailed += LoginManagerOnLoginFailed;
            GwupeClientAppContext.CurrentAppContext.LoginManager.SigningUp += LoginManagerOnSigningUp;
            GwupeClientAppContext.CurrentAppContext.LoginManager.SignupFailed += LoginManagerOnSignupFailed;
            _systray = new SystemTray();
            _systray.Start();
        }

        internal void Start()
        {
            uiThread = new Thread(RunUI) { Name = "uiThread", IsBackground = true };
            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.Start();
            uiReady.WaitOne();
            //LoadBaseSkin();
            if (GwupeClientAppContext.CurrentAppContext.Options.Contains(GwupeOption.Minimize))
            {
                dashBoard.InitWindowHandle();
            }
            else
            {
                Show();
            }
            if (!GwupeClientAppContext.CurrentAppContext.StartupVersion.Equals(GwupeClientAppContext.CurrentAppContext.Reg.LastVersion)
                 && !String.IsNullOrWhiteSpace(GwupeClientAppContext.CurrentAppContext.ChangeDescription))
                SetupAndRunUpdateNotificationWindow();
        }

        public static void LoadBaseSkin()
        {
            try
            {
                Application.Current.Resources.Clear();
                string packUri = String.Format(@"/Skins/Skin.xaml");
                var uri = new Uri(packUri, UriKind.Relative);
                Application.Current.Resources.MergedDictionaries.Add(Application.LoadComponent(uri) as ResourceDictionary);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void RunUI()
        {
            dashBoard = new Dashboard(GwupeClientAppContext.CurrentAppContext);
            uiReady.Set();
            Dispatcher.Run();
        }

        internal void Close()
        {
            if (!IsClosed)
            {
                Logger.Debug("Closing UIManager");
                IsClosed = true;
                if (uiThread != null)
                {
                    dashBoard.Close();
                    uiThread.Abort();
                    uiThread = null;
                }
                if (UpdateNotification != null)
                    UpdateNotification.Close();
                // Remove SystemTray
                if (_systray != null)
                    _systray.Close();
            }
        }

        internal void Show()
        {
            if (dashBoard != null)
                dashBoard.Show();
        }

        internal void Hide()
        {
            dashBoard.Hide();
        }

        internal void ShowDialog(Window dialogWindow)
        {
            dialogWindow.Owner = dashBoard;
            dashBoard.IsEnabled = false;
            dialogWindow.ShowDialog();
            dashBoard.IsEnabled = true;
        }

        private void SetupAndRunUpdateNotificationWindow()
        {
            Thread thread = new Thread(() =>
            {
                UpdateNotification = new UpdateNotification();
                UpdateNotification.Show();
                Dispatcher.Run();
            }) { Name = "updateNotificationThread" };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }
        
        public string RequestElevation(string message)
        {
            return dashBoard.Elevate(message);
        }

        public void CompleteElevation()
        {
            dashBoard.CompleteElevate();
        }

        #region events

        public void ClearDashboardState()
        {
            dashBoard.LoggedIn();
        }

        public void PromptSignup(DataSubmitErrorArgs dataSubmitErrorArgs = null)
        {
            dashBoard.PromptSignup(dataSubmitErrorArgs);
        }

        public GwupeClientAppContext GetAppcontext()
        {
            return GwupeClientAppContext.CurrentAppContext;
        }

        public void PromptLogin()
        {
            dashBoard.Login();
        }

        //public void GetFunctionChat(Function chat)
        //{
        //    _chat = chat;
        //}

        /*
        public void ReceiveNotificationChat(String message,string Flag)
        {
            switch (Flag)
            {
                case "ReceiveFileRequest":
                    _chat
         * '. 
                case "RDPRequest":
                    _chat.LogRdpRequest(message);
                    break;
            }
            
        }*/
         

        //public void GetEngagement(Engagement engagement, EngagementWindow engagementWindow)
        //{
        //    _engagement = engagement;
        //    _engagementWindow = engagementWindow;
       // }

        //public void ShowRDPTerminateButton()
        //{
        //    _engagementWindow.TerminateButtonVisibility = Visibility.Visible;
        //}

        //public void GetRemoteEngagement(Engagement engagement)
        //{
        //    _remoteEngagement = engagement;
        //}
        /*
        public void GetContactRating(int ContactsRating)
        {
            _contactsRating = ContactsRating;
        }

        public int SetContactRating()
        {
            return _contactsRating;
        }*/

        //public Engagement GetSourceObject()
        //{
        //    return _engagement;
        //}

        private void LoginManagerOnLoginFailed(object sender, DataSubmitErrorArgs dataSubmitErrorArgs)
        {
            if (GwupeClientAppContext.CurrentAppContext.IsShuttingDown) return;
            Logger.Debug("Received a login error " + dataSubmitErrorArgs);
            dashBoard.LoginFailed(dataSubmitErrorArgs);
        }

        private void LoginManagerOnLoggingIn(object sender, LoginEventArgs loginEventArgs)
        {
            if (GwupeClientAppContext.CurrentAppContext.IsShuttingDown) return;
            if (loginEventArgs.LoginState == LoginState.LoggingIn)
                dashBoard.LoggingIn();
        }

        private void LoginManagerOnLoggedOut(object sender, LoginEventArgs loginEventArgs)
        {
            if (GwupeClientAppContext.CurrentAppContext.IsShuttingDown) return;
            if (loginEventArgs.LoginState == LoginState.LoggedOut)
                dashBoard.Login();
        }

        private void LoginManagerOnLoggedIn(object sender, LoginEventArgs loginEventArgs)
        {
            if (GwupeClientAppContext.CurrentAppContext.IsShuttingDown) return;
            if (loginEventArgs.LoginState == LoginState.LoggedIn)
                dashBoard.LoggedIn();
        }

        private void LoginManagerOnSigningUp(object sender, EventArgs eventArgs)
        {
            if (GwupeClientAppContext.CurrentAppContext.IsShuttingDown) return;
            dashBoard.SigningUp();
        }

        private void LoginManagerOnSignupFailed(object sender, DataSubmitErrorArgs dataSubmitErrorArgs)
        {
            if (GwupeClientAppContext.CurrentAppContext.IsShuttingDown) return;
            dashBoard.PromptSignup(dataSubmitErrorArgs);
        }

        //public void StopRemoteConnection()
        //{
        //    Thread thread = new Thread(((Components.Functions.RemoteDesktop.Function)_remoteEngagement.GetFunction("RemoteDesktop")).Server.Close) { IsBackground = true };
        //    thread.Start();
        //}

        #endregion

        public void Alert(string message)
        {
            dashBoard.Alert(message);
        }

        public FaultReport GenerateFaultReport()
        {
            return dashBoard.GenerateFaultReport();
        }

        public void PromptGuestSignup()
        {
            Alert("Please note, for security reasons, this functionality is restricted to logged in users.  But don't worry, sign up for a FREE account to get access right now.");
            PromptSignup();
        }
    }
}
