using System;
using System.Threading;
using BlitsMe.Agent.Components;
using BlitsMe.Agent.Exceptions;
using BlitsMe.Agent.Misc;
using BlitsMe.Cloud.Exceptions;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using BlitsMe.Common.Security;
using log4net;

namespace BlitsMe.Agent.Managers
{
    internal delegate void LoginEvent(object sender, LoginEventArgs e);

    public class LoginManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LoginManager));
        private Thread _loginManagerThread;
        internal bool IsClosed { get; private set; }
        private readonly BLMRegistry _reg = new BLMRegistry();
        private readonly AutoResetEvent _signinEvent = new AutoResetEvent(false);
        private readonly BlitsMeClientAppContext _appContext;
        public bool IsLoggedIn = false;
        public bool IsLoggingIn = false;

        public Object LoginOccurredLock { get; private set; }
        public Object LogoutOccurredLock { get; private set; }
        public LoginDetails LoginDetails = new LoginDetails();

        private Object LoginReadyLock = new Object();
        private Object ConnectionReadyLock = new Object();

        /// <summary>
        /// The even which drives if we are connected, ready to login
        /// </summary>
        private readonly AutoResetEvent _connectionReady;

        /// <summary>
        /// The event which drives are we ready to login yet
        /// </summary>
        private readonly AutoResetEvent _loginDetailsReady = new AutoResetEvent(false);

        /// <summary>
        /// Fired when a logout attempt occurs
        /// </summary>
        internal event LoginEvent LoggingOut;

        /// <summary>
        /// Fired when a login attempt occurs
        /// </summary>
        internal event LoginEvent LoggingIn;

        /// <summary>
        /// Fired when a login occurs
        /// </summary>
        internal event LoginEvent LoggedIn;

        /// <summary>
        /// Fired when a logout occurs
        /// </summary>
        internal event LoginEvent LoggedOut;

        /// <summary>
        /// Fired when a login fails
        /// </summary>
        internal event EventHandler<DataSubmitErrorArgs> LoginFailed;

        /// <summary>
        /// Fired when a signup fails
        /// </summary>
        internal event EventHandler<DataSubmitErrorArgs> SignupFailed;

        /// <summary>
        /// Fired when a signup attempt occurs.
        /// </summary>
        internal event EventHandler SigningUp;

        #region Event Invocators

        private void OnSignupFailed(DataSubmitErrorArgs e)
        {
            if (_appContext.IsShuttingDown) return;
            EventHandler<DataSubmitErrorArgs> handler = SignupFailed;
            if (handler != null) handler(this, e);
        }

        private void OnSigningUp()
        {
            EventHandler handler = SigningUp;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        private void OnLoginFailed(String fieldName, String errorCode)
        {
            if (_appContext.IsShuttingDown) return;
            var error = new DataSubmitError() { FieldName = fieldName, ErrorCode = errorCode };
            DataSubmitErrorArgs e = new DataSubmitErrorArgs() { SubmitErrors = { error } };
            EventHandler<DataSubmitErrorArgs> handler = LoginFailed;
            if (handler != null) handler(this, e);
        }

        private void OnLoggingOut()
        {
            if (_appContext.IsShuttingDown) return;
            LoginEventArgs e = new LoginEventArgs { LoginState = LoginState.LoggingOut };
            LoginEvent handler = LoggingOut;
            if (handler != null) handler(this, e);
        }

        private void OnLoggingIn()
        {
            if (_appContext.IsShuttingDown) return;
            LoginEventArgs e = new LoginEventArgs { LoginState = LoginState.LoggingIn };
            LoginEvent handler = LoggingIn;
            if (handler != null) handler(this, e);
        }

        private void OnLoggedOut()
        {
            if (_appContext.IsShuttingDown) return;
            LoginEventArgs e = new LoginEventArgs { LoginState = LoginState.LoggedOut };
            LoginEvent handler = LoggedOut;
            if (handler != null) handler(this, e);
        }

        private void OnLoggedIn()
        {
            if (_appContext.IsShuttingDown) return;
            LoginEventArgs e = new LoginEventArgs { LoginState = LoginState.LoggedIn };
            LoginEvent handler = LoggedIn;
            if (handler != null) handler(this, e);
        }

        #endregion

        public LoginManager()
        {
            this._appContext = BlitsMeClientAppContext.CurrentAppContext;
            LoginOccurredLock = new Object();
            LogoutOccurredLock = new Object();
            if (!String.IsNullOrEmpty(_reg.Username))
            {
                LoginDetails.Username = _reg.Username;
                if (!String.IsNullOrEmpty(_reg.PasswordHash))
                {
                    LoginDetails.PasswordHash = _reg.PasswordHash;
                    // now we can initiate a login
                    _loginDetailsReady.Set();
                }
            }
            _connectionReady = new AutoResetEvent(false);
            // Link into the connect and disconnect event handlers
            _appContext.ConnectionManager.Connect += Connected;
            _appContext.ConnectionManager.Disconnect += Disconnected;
        }

        public void Start()
        {
            _loginManagerThread = new Thread(Run) { IsBackground = true, Name = "_loginManagerThread" };
            _loginManagerThread.Start();
        }

        public void Close()
        {
            if (!IsClosed)
            {
                IsClosed = true;
                if (_loginManagerThread != null && _loginManagerThread.IsAlive)
                    _loginManagerThread.Abort();
                if (IsLoggedIn)
                {
                    Logout();
                }
            }
        }


        private void Connected(Object sender, EventArgs e)
        {
            if (_appContext.IsShuttingDown) return;
#if DEBUG
            Logger.Debug("Connected, marking login as required");
#endif
            lock (ConnectionReadyLock) Monitor.PulseAll(ConnectionReadyLock);
        }

        private void Disconnected(Object sender, EventArgs e)
        {
            if (_appContext.IsShuttingDown) return;
#if DEBUG
            Logger.Debug("disconnected, flagging logout occurred");
#endif
            Logout(false);
            lock (ConnectionReadyLock) Monitor.PulseAll(ConnectionReadyLock);
        }

        public void Logout(bool clearPassword = true)
        {
            if (IsLoggedIn)
            {
                OnLoggingOut();
                if (_appContext.ConnectionManager.Connection.isEstablished())
                {
                    LogoutRq request = new LogoutRq();
                    try
                    {
                        _appContext.ConnectionManager.Connection.Request<LogoutRq, LogoutRs>(request);
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Failed to logout correctly : " + e.Message, e);
                    }
                }
                if (clearPassword)
                    LoginDetails.PasswordHash = "";
                InvalidateSession();
            }
        }

        public void InvalidateSession()
        {
            IsLoggedIn = false;
            // Lets pulse the logout occurred lock
            lock (LogoutOccurredLock)
            {
                Monitor.PulseAll(LogoutOccurredLock);
            }
            OnLoggedOut();
            lock (LoginReadyLock) Monitor.PulseAll(LoginReadyLock);
        }

        public void Run()
        {
            // Setup the connection marking information
            LoginDetails.Profile = _reg.Profile;
            try
            {
                LoginDetails.Workstation = _appContext.BlitsMeService.HardwareFingerprint();
            }
            catch (Exception e)
            {
                Logger.Error("Failed to get the hardware id : " + e.Message);
            }
            if (!LoginDetails.Ready)
                OnLoginFailed(null, "INCOMPLETE");
            // Main logging in thread
            while (true)
            {
                // Wait for the login details to become ready
                lock (LoginReadyLock)
                    while (!LoginDetails.Ready || IsLoggedIn)
                    {
                        Monitor.Wait(LoginReadyLock);
                    }
                try
                {
                    IsLoggingIn = true;
                    OnLoggingIn();
                    Logger.Debug("Login details ready, waiting for connection");
                    // Wait for the connection to become ready
                    lock (ConnectionReadyLock) while (!_appContext.ConnectionManager.Connection.isEstablished()) Monitor.Wait(ConnectionReadyLock);
                    Logger.Debug("Connection ready, attempting login");

                    try
                    {
                        // Attempt a login
                        ProcessLogin();
                    }
                    catch (LoginException e)
                    {
                        Logger.Warn("Login has failed : " + e.Message);
                        // Login failed with authfailure, we reset the password so it will be prompted for again, otherwise we just try login again
                        if (e.AuthFailure)
                        {
                            LoginDetails.PasswordHash = "";
                            OnLoginFailed("PasswordHash", "INCORRECT");
                        }
                        else
                        {
                            // Failed for another reason, lets retry after 10 seconds (loop test will check login details and connection readiness
                            OnLoginFailed(null, e.Failure);
                            Thread.Sleep(10000);
                        }
                    }
                    catch (Exception e)
                    {
                        // Do nothing here, just try keep connecting
                        Logger.Warn("Login has failed : " + e.Message, e);
                        OnLoginFailed(null, "UNKNOWN_ERROR");
                        Thread.Sleep(10000);
                    }

                }
                finally
                {
                    IsLoggingIn = false;
                }
            }
        }

        internal void Login(String username, String password)
        {
            LoginDetails.Username = username;
            LoginDetails.PasswordHash = Util.getSingleton().hashPassword(password);
            lock (LoginReadyLock) Monitor.PulseAll(LoginReadyLock);
        }

        private void ProcessLogin()
        {
            var loginRq = new LoginRq
                {
                    username = LoginDetails.Username,
                    passwordDigest = LoginDetails.PasswordHash,
                    profile = LoginDetails.Profile,
                    workstation = LoginDetails.Workstation,
                    version = _appContext.Version(2),
                    partnerCode = _appContext.Reg.getRegValue("PromoCode", true),
                };
            LoginRs loginRs = null;
            try
            {
                loginRs = _appContext.ConnectionManager.Connection.Request<LoginRq, LoginRs>(loginRq);
                _appContext.RosterManager.RetrieveRoster();
                _appContext.CurrentUserManager.SetUser(loginRs.userElement, loginRs.shortCode);
                if (loginRs.partnerElement != null)
                {
                    _appContext.SettingsManager.Partner = new Partner()
                    {
                        Basename = loginRs.partnerElement.basename,
                        LinkText = loginRs.partnerElement.linkText,
                        Logo = Convert.FromBase64String(loginRs.partnerElement.logo),
                        Name = loginRs.partnerElement.name,
                        Text = loginRs.partnerElement.text,
                        Website = loginRs.partnerElement.website
                    };
                }
                else
                {
                    _appContext.SettingsManager.Partner = null;
                }
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
            _reg.Username = LoginDetails.Username;
            _reg.PasswordHash = LoginDetails.PasswordHash;
            _reg.Profile = LoginDetails.Profile = loginRs.profileId;
            IsLoggedIn = true;
            // Notify all threads waiting for login
            lock (LoginOccurredLock)
            {
                Monitor.PulseAll(LoginOccurredLock);
            }
            OnLoggedIn();
            Logger.Info("Login success : " + _appContext.CurrentUserManager.CurrentUser.Username + "!" + _appContext.CurrentUserManager.ActiveShortCode + "@" + LoginDetails.Profile + "-" +
                        LoginDetails.Workstation);

        }

        public void Signup(String firstname, String lastname, String username, String password, String email, String location, bool supporter)
        {
            var request = new SignupRq
            {
                firstname = firstname.Trim(),
                lastname = lastname.Trim(),
                username = username.Trim(),
                password = password,
                email = email.Trim(),
                location = location.Trim(),
                supporter = supporter
            };
            OnSigningUp();
            try
            {
                _appContext.ConnectionManager.Connection.Request<SignupRq, SignupRs>(request);
                Login(request.username, request.password);
            }
            catch (MessageException<SignupRs> ex)
            {
                var errors = new DataSubmitErrorArgs();
                Logger.Warn("Failed to signup : " + ex.Message);
                if (ex.Response.signupErrors != null)
                {
                    if (ex.Response.signupErrors.Contains(SignupRs.SignupErrorEmailAddressInUse))
                    {
                        errors.SubmitErrors.Add(new DataSubmitError()
                        {
                            FieldName = "email",
                            ErrorCode = SignupRs.SignupErrorEmailAddressInUse
                        });
                    }
                    if (ex.Response.signupErrors.Contains(SignupRs.SignupErrorUserExists))
                    {
                        errors.SubmitErrors.Add(new DataSubmitError()
                        {
                            FieldName = "username",
                            ErrorCode = SignupRs.SignupErrorUserExists
                        });
                    }
                    if (ex.Response.signupErrors.Contains(SignupRs.SignupErrorPasswordComplexity))
                    {
                        errors.SubmitErrors.Add(new DataSubmitError()
                        {
                            FieldName = "password",
                            ErrorCode = SignupRs.SignupErrorPasswordComplexity
                        });
                    }
                }
                OnSignupFailed(errors);
            }
            catch (Exception ex)
            {
                Logger.Warn("Failed to signup : " + ex.Message);
                OnSignupFailed(new DataSubmitErrorArgs());
            }
        }
    }
}