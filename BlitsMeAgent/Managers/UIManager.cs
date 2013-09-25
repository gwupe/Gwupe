using System.Threading;
using System.Windows;
using System.Windows.Threading;
using BlitsMe.Agent.Components;
using BlitsMe.Agent.UI.WPF;
using log4net;

namespace BlitsMe.Agent.Managers
{
    internal class UIManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (UIManager));
        private Thread uiManagerThread;
        private Thread uiThread;
        private AutoResetEvent uiReady;
        private Dashboard dashBoard;
        internal UpdateNotification UpdateNotification;
        internal Window CurrentWindow { get { return dashBoard; } }
        internal bool IsClosed { get; private set; }

        internal UIManager()
        {
            uiReady = new AutoResetEvent(false);
            BlitsMeClientAppContext.CurrentAppContext.LoginManager.LoggedIn += LoginManagerOnLoggedIn;
            BlitsMeClientAppContext.CurrentAppContext.LoginManager.LoggedOut += LoginManagerOnLoggedOut;
            BlitsMeClientAppContext.CurrentAppContext.LoginManager.LoggingIn += LoginManagerOnLoggingIn;
            BlitsMeClientAppContext.CurrentAppContext.LoginManager.LoginFailed += LoginManagerOnLoginFailed;
        }

        internal void Start()
        {
            uiThread = new Thread(RunUI) {Name = "uiThread", IsBackground = true};
            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.Start();
            uiReady.WaitOne();
            Show();
            if (!BlitsMeClientAppContext.CurrentAppContext.StartupVersion.Equals(BlitsMeClientAppContext.CurrentAppContext.Reg.LastVersion))
                SetupAndRunUpdateNotificationWindow();
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
            dashBoard.Show();
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

        private void LoginManagerOnLoginFailed(object sender, DataSubmitErrorArgs dataSubmitErrorArgs)
        {
            if (BlitsMeClientAppContext.CurrentAppContext.IsShuttingDown) return;
            Logger.Debug("Received a login error " + dataSubmitErrorArgs);
            if (dataSubmitErrorArgs.HasErrorField("PasswordHash"))
            {
                Logger.Debug("Password was incorrect, showing login screen");
                dashBoard.ShowLoginScreen(true);
            }
            else if (dataSubmitErrorArgs.HasError("INCOMPLETE"))
            {
                Logger.Debug("Data is incomplete, showing login screen");
                dashBoard.ShowLoginScreen();
            }
        }

        private void LoginManagerOnLoggingIn(object sender, LoginEventArgs loginEventArgs)
        {
            if (BlitsMeClientAppContext.CurrentAppContext.IsShuttingDown) return;
            if (loginEventArgs.LoginState == LoginState.LoggingIn)
            {
                Logger.Debug("Showing logging in screen");
                dashBoard.LoggingInScreen = true;
            }
        }

        private void LoginManagerOnLoggedOut(object sender, LoginEventArgs loginEventArgs)
        {
            if (BlitsMeClientAppContext.CurrentAppContext.IsShuttingDown) return;
            if (loginEventArgs.LoginState == LoginState.LoggedOut)
                dashBoard.ShowLoginScreen();
        }

        private void LoginManagerOnLoggedIn(object sender, LoginEventArgs loginEventArgs)
        {
            if (BlitsMeClientAppContext.CurrentAppContext.IsShuttingDown) return;
            if (loginEventArgs.LoginState == LoginState.LoggedIn)
            {
                dashBoard.LoggingInScreen = false;
            }
        }

        #endregion

    }
}
