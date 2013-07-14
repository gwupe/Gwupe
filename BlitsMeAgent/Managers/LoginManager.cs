using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using BlitsMe.Agent.Components;
using BlitsMe.Agent.Misc;
using BlitsMe.Agent.UI.WPF;
using BlitsMe.Cloud.Exceptions;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using log4net;

namespace BlitsMe.Agent.Managers
{
    public delegate void LoginEvent(object sender, LoginEventArgs e);

    public class LoginManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LoginManager));
        private Thread _loginManagerThread;
        private readonly AutoResetEvent _loginUiReadyEvent = new AutoResetEvent(false);
        private Thread _loginUiThread;
        private readonly BLMRegistry _reg = new BLMRegistry();
        private readonly AutoResetEvent _signinEvent = new AutoResetEvent(false);
        private readonly BlitsMeClientAppContext _appContext;
        public bool IsLoggedIn = false;
        public bool IsLoggingIn = false;
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
            LoginDetails = new LoginDetails(_reg.Username, _reg.PasswordHash);
            // Event Handlers
            _appContext.ConnectionManager.Connect += Connected;
            _appContext.ConnectionManager.Disconnect += Disconnected;
        }

        public void Start()
        {
            // UI Thread
            _loginUiThread = new Thread(RunLoginUi) { Name = "_loginUiThread", IsBackground = true };
            _loginUiThread.SetApartmentState(ApartmentState.STA);
            _loginUiThread.Start();
            // Wait for the Login UI to be initialised
            _loginUiReadyEvent.WaitOne();
            // Manager thread
            _loginManagerThread = new Thread(Run) { IsBackground = true, Name = "_loginManagerThread" };
            _loginManagerThread.Start();
        }

        public LoginDetails LoginDetails { get; set; }

        private void RunLoginUi()
        {
            _loginWindow = new LoginWindow(_appContext, LoginDetails, _signinEvent);
            _loginUiReadyEvent.Set();
            if (!_appContext.Options.Contains(BlitsMeOption.Minimize))
            {
                _loginWindow.Show();
            }
            Dispatcher.Run();
        }

        public void Close()
        {
            if (IsLoggedIn)
                Logout(true);
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
            Logout(false);
        }

        public void Logout(bool userInitiated)
        {
            if (!_appContext.IsShuttingDown)
            {
                _appContext.EngagementManager.Reset();
                _appContext.CurrentUserManager.Reset();
                _appContext.NotificationManager.Reset();
                _appContext.RosterManager.Reset();
                _appContext.P2PManager.Reset();
                _appContext.SearchManager.Reset();
                if (_appContext.UIDashBoard != null)
                { _appContext.UIDashBoard.Reset(); }
            }
            if (userInitiated)
            {
                if (_loginWindow.Dispatcher.CheckAccess())
                {
                    _loginWindow.Username.Text = LoginDetails.username;
                    _loginWindow.Password.Password = "";
                }
                else
                {
                    _loginWindow.Dispatcher.Invoke(new Action(() =>
                    {
                        _loginWindow.Username.Text = LoginDetails.username;
                        _loginWindow.Password.Password = "";
                    }));
                }
                LoginDetails.username = "";
                LoginDetails.passwordHash = "";
            }
            if (IsLoggedIn)
            {
                if (_appContext.ConnectionManager.Connection.isEstablished())
                {
                    LogoutRq request = new LogoutRq();
                    //_appContext.ConnectionManager.Connection.RequestAsync<LogoutRq,LogoutRs>(request, delegate {  });
                    try
                    {
                        _appContext.ConnectionManager.Connection.Request<LogoutRq, LogoutRs>(request);
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Failed to logout correctly : " + e.Message, e);
                    }
                }
                IsLoggedIn = false;
                // Lets pulse the logout occurred lock
                lock (LogoutOccurredLock)
                {
                    Monitor.PulseAll(LogoutOccurredLock);
                }
                OnLoggedOut(new LoginEventArgs() { Logout = true });
                if (_appContext.ConnectionManager.Connection.isEstablished())
                {
                    LoginRequired.Set();
                }
            }
        }

        public void Run()
        {
            // Lets collect the information on startup if we have to.
            if (LoginDetails.username == null || LoginDetails.username.Equals("") || LoginDetails.passwordHash == null ||
                LoginDetails.passwordHash.Equals(""))
            {
                // get the username from the UI
                ShowLoginWindow();
                _loginWindow.SignalPleaseLogin();
                _signinEvent.WaitOne();
#if DEBUG
                Logger.Debug("Login window signalled login");
#endif
                _loginWindow.SignalLoggingIn();
            }
            LoginDetails.profile = _reg.Profile;
            try
            {
                LoginDetails.workstation = _appContext.BlitsMeService.HardwareFingerprint();
            }
            catch (Exception e)
            {
                Logger.Error("Failed to get the hardware id : " + e.Message);
            }
            while (true)
            {
                // Lets wait for the login required event
                LoginRequired.WaitOne();
#if DEBUG
                Logger.Debug("Connection signalled login required");
#endif
                while (!IsLoggedIn && _appContext.ConnectionManager.Connection.isEstablished())
                {
                    if (LoginDetails.username == null || LoginDetails.username.Equals("") ||
                        LoginDetails.passwordHash == null || LoginDetails.passwordHash.Equals(""))
                    {
                        // get the username from the UI ( show the login window )
                        ShowLoginWindow();
                        _loginWindow.SignalPleaseLogin();
                        _signinEvent.WaitOne();
#if DEBUG
                        Logger.Debug("Login window signalled login");
#endif
                        // hide the login window
                        _loginWindow.SignalLoggingIn();
                    }
                    try
                    {
                        Login();
                        if (_loginWindow.Visibility == Visibility.Visible)
                        {
                            HideLoginWindow();
                            _appContext.UIDashBoard.Show();
                        }
                    }
                    catch (LoginException e)
                    {
                        _loginWindow.SignalPleaseLogin();
                        Logger.Warn("Login has failed : " + e.Message);
                        // Login failed with authfailure, we reset the password so it will be prompted for again, otherwise we just try login again
                        if (e.authFailure)
                        {
                            LoginDetails.passwordHash = "";
                            if (_loginWindow.Dispatcher.CheckAccess())
                                _loginWindow.LoginFailed();
                            else
                                _loginWindow.Dispatcher.Invoke(new Action(() => _loginWindow.LoginFailed()));
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
                        Logger.Warn("Login has failed : " + e.Message, e);
                        Thread.Sleep(10000);
                    }
                }
            }
        }

        internal void HideLoginWindow()
        {
            if (_loginWindow.Dispatcher.CheckAccess())
                _loginWindow.Hide();
            else
                _loginWindow.Dispatcher.Invoke(new Action(() => _loginWindow.Hide()));
        }

        internal void ShowLoginWindow()
        {
            if (_loginWindow.Dispatcher.CheckAccess())
            {
                _appContext.UIDashBoard.Hide();
                _loginWindow.Show();
                _loginWindow.Topmost = true;
                _loginWindow.Topmost = false;
                _loginWindow.Focus();
            }
            else
                _loginWindow.Dispatcher.Invoke(new Action(ShowLoginWindow));
        }


        private void Login()
        {
            if (_appContext.ConnectionManager.Connection.isEstablished())
            {
                var loginRq = new LoginRq
                    {
                        username = LoginDetails.username,
                        passwordDigest = LoginDetails.passwordHash,
                        profile = LoginDetails.profile,
                        workstation = LoginDetails.workstation,
                        version = _appContext.Version(2)
                    };
                LoginRs loginRs = null;
                try
                {
                    loginRs = _appContext.ConnectionManager.Connection.Request<LoginRq, LoginRs>(loginRq);
                    _appContext.RosterManager.RetrieveRoster();
                    _appContext.CurrentUserManager.SetUser(loginRs.userElement);
                }
                catch (MessageException<LoginRs> e)
                {
                    if (!e.Response.loggedIn)
                    {
                        throw new LoginException("Failed to login, server responded with : " + e.Response.errorMessage,
                                                 e.Response.error);
                    }
                    throw;
                }
                // Exception not thrown, login success, save details
                _reg.Username = LoginDetails.username;
                _reg.PasswordHash = LoginDetails.passwordHash;
                _reg.Profile = LoginDetails.profile = loginRs.profileId;
                LoginDetails.shortCode = loginRs.shortCode;
                IsLoggedIn = true;
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
}