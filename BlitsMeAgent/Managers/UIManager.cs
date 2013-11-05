using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using BlitsMe.Agent.Components;
using BlitsMe.Agent.Components.Functions.Chat;
using BlitsMe.Agent.Components.Search;
using BlitsMe.Agent.UI.WPF;
using log4net;

namespace BlitsMe.Agent.Managers
{
    internal class UIManager : Application
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(UIManager));
        private Thread uiManagerThread;
        private Thread uiThread;
        private AutoResetEvent uiReady;
        private Dashboard dashBoard;
        internal UpdateNotification UpdateNotification;
        internal Window CurrentWindow { get { return dashBoard; } }
        internal bool IsClosed { get; private set; }
        private Engagement _engagement;
        private Engagement _remoteEngagement;
        private Function _chat;

        internal UIManager()
        {
            uiReady = new AutoResetEvent(false);
            BlitsMeClientAppContext.CurrentAppContext.LoginManager.LoggedIn += LoginManagerOnLoggedIn;
            BlitsMeClientAppContext.CurrentAppContext.LoginManager.LoggedOut += LoginManagerOnLoggedOut;
            BlitsMeClientAppContext.CurrentAppContext.LoginManager.LoggingIn += LoginManagerOnLoggingIn;
            BlitsMeClientAppContext.CurrentAppContext.LoginManager.LoginFailed += LoginManagerOnLoginFailed;
            BlitsMeClientAppContext.CurrentAppContext.LoginManager.SigningUp += LoginManagerOnSigningUp;
            BlitsMeClientAppContext.CurrentAppContext.LoginManager.SignupFailed += LoginManagerOnSignupFailed;
        }

        internal void Start()
        {
            uiThread = new Thread(RunUI) { Name = "uiThread", IsBackground = true };
            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.Start();
            uiReady.WaitOne();
            if (BlitsMeClientAppContext.CurrentAppContext.Options.Contains(BlitsMeOption.Minimize))
            {
                dashBoard.InitWindowHandle();
            }
            else
            {
                LoadBaseSkin();
                Show();
            }
            if (!BlitsMeClientAppContext.CurrentAppContext.StartupVersion.Equals(BlitsMeClientAppContext.CurrentAppContext.Reg.LastVersion)
                 && !String.IsNullOrWhiteSpace(BlitsMeClientAppContext.CurrentAppContext.ChangeDescription))
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
            dashBoard = new Dashboard(BlitsMeClientAppContext.CurrentAppContext);
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
            dialogWindow.Owner = CurrentWindow;
            dialogWindow.ShowDialog();
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

        #region events

        public void PromptSignup(DataSubmitErrorArgs dataSubmitErrorArgs = null)
        {
            dashBoard.PromptSignup(dataSubmitErrorArgs);
        }

        public BlitsMeClientAppContext GetAppcontext()
        {
            return BlitsMeClientAppContext.CurrentAppContext;
        }

        public void PromptLogin()
        {
            dashBoard.Login(false);
        }

        public void GetFunctionChat(Function chat)
        {
            _chat = chat;
        }

        public void ReceiveNotificationChat(String message)
        {
            _chat.LogSecondPartySystemNotification(message);
        }

        public void GetEngagement(Engagement engagement)
        {
            _engagement = engagement;
        }

        public void GetRemoteEngagement(Engagement engagement)
        {
            _remoteEngagement = engagement;
        }

        public Engagement GetSourceObject()
        {
            return _engagement;
        }

        private void LoginManagerOnLoginFailed(object sender, DataSubmitErrorArgs dataSubmitErrorArgs)
        {
            if (BlitsMeClientAppContext.CurrentAppContext.IsShuttingDown) return;
            Logger.Debug("Received a login error " + dataSubmitErrorArgs);
            if (dataSubmitErrorArgs.HasErrorField("PasswordHash"))
            {
                Logger.Debug("Password was incorrect, showing login screen");
                dashBoard.Login(true);
            }
            else if (dataSubmitErrorArgs.HasError("INCOMPLETE"))
            {
                Logger.Debug("Data is incomplete, showing login screen");
                dashBoard.Login();
            }
        }

        private void LoginManagerOnLoggingIn(object sender, LoginEventArgs loginEventArgs)
        {
            if (BlitsMeClientAppContext.CurrentAppContext.IsShuttingDown) return;
            if (loginEventArgs.LoginState == LoginState.LoggingIn)
                dashBoard.LoggingIn();
        }

        private void LoginManagerOnLoggedOut(object sender, LoginEventArgs loginEventArgs)
        {
            if (BlitsMeClientAppContext.CurrentAppContext.IsShuttingDown) return;
            if (loginEventArgs.LoginState == LoginState.LoggedOut)
                dashBoard.Login();
        }

        private void LoginManagerOnLoggedIn(object sender, LoginEventArgs loginEventArgs)
        {
            if (BlitsMeClientAppContext.CurrentAppContext.IsShuttingDown) return;
            if (loginEventArgs.LoginState == LoginState.LoggedIn)
                dashBoard.LoggedIn();
        }

        private void LoginManagerOnSigningUp(object sender, EventArgs eventArgs)
        {
            if (BlitsMeClientAppContext.CurrentAppContext.IsShuttingDown) return;
            dashBoard.SigningUp();
        }

        private void LoginManagerOnSignupFailed(object sender, DataSubmitErrorArgs dataSubmitErrorArgs)
        {
            if (BlitsMeClientAppContext.CurrentAppContext.IsShuttingDown) return;
            dashBoard.PromptSignup(dataSubmitErrorArgs);
        }

        public void StopRemoteConnection()
        {
            Thread thread = new Thread(((Components.Functions.RemoteDesktop.Function)_remoteEngagement.GetFunction("RemoteDesktop")).Server.Close) { IsBackground = true };
            thread.Start();
        }

        #endregion

    }
}
