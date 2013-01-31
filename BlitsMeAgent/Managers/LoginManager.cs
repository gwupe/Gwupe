using System;
using System.Threading;
using System.Windows.Threading;
using BlitsMe.Agent.Components;
using BlitsMe.Agent.Misc;
using BlitsMe.Agent.UI.WPF;
using BlitsMe.Cloud.Exceptions;
using log4net;

namespace BlitsMe.Agent.Managers
{
    public delegate void LoginEvent(object sender, LoginEventArgs e);

    public class LoginManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (LoginManager));
        private readonly Thread _loginManagerThread;
        private readonly AutoResetEvent _loginUiReadyEvent = new AutoResetEvent(false);
        private readonly Thread _loginUiThread;
        private readonly BLMRegistry _reg = new BLMRegistry();
        private readonly AutoResetEvent _signinEvent = new AutoResetEvent(false);
        private readonly BlitsMeClientAppContext _appContext;
        public AutoResetEvent LoginRequired = new AutoResetEvent(false);

        public Object LoginOccurredLock { get; private set; }
        public Object LogoutOccurredLock { get; private set; }

        public event LoginEvent LoggedIn;
        public event LoginEvent LoggedOut;

        private void OnLoggedOut(LoginEventArgs e)
        {
            LoginEvent handler = LoggedOut;
            if (handler != null) handler(this, e);
        }

        private void OnLoggedIn(LoginEventArgs e)
        {
            LoginEvent handler = LoggedIn;
            if (handler != null) handler(this, e);
        }

        private LoginWindow _loginWindow;

        public LoginManager(BlitsMeClientAppContext appContext)
        {
            this._appContext = appContext;
            LoginOccurredLock = new Object();
            LogoutOccurredLock = new Object();
            LoginDetails = new LoginDetails(_reg.username, _reg.passwordHash);
            // Event Handlers
            _appContext.ConnectionManager.Connect += Connected;
            _appContext.ConnectionManager.Disconnect += Disconnected;
            // UI Thread
            _loginUiThread = new Thread(RunLoginUi) { Name = "_loginUiThread", IsBackground = true };
            _loginUiThread.SetApartmentState(ApartmentState.STA);
            _loginUiThread.Start();
            // Manager thread
            _loginManagerThread = new Thread(Run) { IsBackground = true, Name = "_loginManagerThread" };
            _loginManagerThread.Start();
        }

        public LoginDetails LoginDetails { get; set; }

        private void RunLoginUi()
        {
            _loginWindow = new LoginWindow(LoginDetails, _signinEvent);
            _loginUiReadyEvent.Set();
            Dispatcher.Run();
        }

        public void Close()
        {

            _loginWindow.Dispatcher.InvokeShutdown();
            _loginUiThread.Abort();
            _loginManagerThread.Abort();
        }

        private void Connected(Object sender, EventArgs e)
        {
#if DEBUG
            Logger.Debug("Connected, marking login as required");
#endif
            LoginRequired.Set();
        }

        private void Disconnected(Object sender, EventArgs e)
        {
#if DEBUG
            Logger.Debug("disconnected, flagging logout occurred");
#endif
            // Lets pulse the logout occurred lock
            lock (LogoutOccurredLock)
            {
                Monitor.PulseAll(LogoutOccurredLock);
            }
            OnLoggedOut(new LoginEventArgs() { Logout = true });
        }

        public void Run()
        {
            // Wait for the Login UI to be initialised
            _loginUiReadyEvent.WaitOne();
            // Lets collect the information on startup if we have to.
            if (LoginDetails.username == null || LoginDetails.username.Equals("") || LoginDetails.passwordHash == null ||
                LoginDetails.passwordHash.Equals(""))
            {
                // get the username from the UI
                _loginWindow.Dispatcher.Invoke(new Action(() => _loginWindow.Show()), null);
                _signinEvent.WaitOne();
#if DEBUG
                Logger.Debug("Login window signalled login");
#endif
                _loginWindow.Dispatcher.Invoke(new Action(() => _loginWindow.Hide()), null);
            }
            LoginDetails.profile = _reg.profile;
            LoginDetails.workstation = _reg.workstation;
            while (true)
            {
                // Lets wait for the login required event
                LoginRequired.WaitOne();
#if DEBUG
                Logger.Debug("Connection signalled login required");
#endif
                _appContext.ConnectionManager.Connection.isLoggedIn = false;
                while (!_appContext.ConnectionManager.Connection.isLoggedIn && _appContext.ConnectionManager.Connection.isEstablished())
                {
                    if (LoginDetails.username == null || LoginDetails.username.Equals("") ||
                        LoginDetails.passwordHash == null || LoginDetails.passwordHash.Equals(""))
                    {
                        // get the username from the UI ( show the login window )
                        _loginWindow.Dispatcher.Invoke(new Action(() => _loginWindow.Show()), null);
                        _signinEvent.WaitOne();
#if DEBUG
                        Logger.Debug("Login window signalled login");
#endif
                        // hide the login window
                        _loginWindow.Dispatcher.Invoke(new Action(() => _loginWindow.Hide()), null);
                    }
                    try
                    {
                            _login();
                    }
                    catch (LoginException e)
                    {
                        Logger.Warn("Login has failed : " + e.Message);
                        // Login failed with authfailure, we reset the password so it will be prompted for again, otherwise we just try login again
                        if (e.authFailure)
                        {
                            LoginDetails.passwordHash = "";
                            _loginWindow.Dispatcher.Invoke(new Action(() => _loginWindow.loginFailed()), null);
                        }
                        else
                        {
                            // Failed for another reason, lets retry after 10 seconds
                            Thread.Sleep(10000);
                        }
                    }
                    catch (Exception e)
                    {
                        // Do nothing here, just try keep connecting
                        Logger.Warn("Login has failed : " + e.Message);
                        Thread.Sleep(10000);
                    }
                }
            }
        }

        private void _login()
        {
            // Attempt to login
            _appContext.ConnectionManager.Connection.login(LoginDetails.username, LoginDetails.profile, LoginDetails.workstation,
                                                           LoginDetails.passwordHash);
            // Exception not thrown, login success, save details
            _reg.username = LoginDetails.username;
            _reg.passwordHash = LoginDetails.passwordHash;
            _reg.profile = LoginDetails.profile = _appContext.ConnectionManager.Connection.loginRs.profileId;
            LoginDetails.shortCode = _appContext.ConnectionManager.Connection.loginRs.shortCode;
            // Notify all threads waiting for login
            lock (LoginOccurredLock)
            {
                Monitor.PulseAll(LoginOccurredLock);
            }
            OnLoggedIn(new LoginEventArgs() { Login = true });
            Logger.Info("Login success : " + LoginDetails.username + "@" + LoginDetails.profile + "-" +
                        LoginDetails.workstation);
        }
    }
}