using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Navigation;
using System.Windows.Media;
using System.Threading;
using System.ComponentModel;
using BlitsMe.Agent.Components;
using BlitsMe.Agent.UI.WPF.Utils;
using BlitsMe.Common.Security;
using log4net;

namespace BlitsMe.Agent.UI.WPF
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LoginWindow));
        private readonly BlitsMeClientAppContext _appContext;
        private readonly LoginDetails _loginDetails;
        public AutoResetEvent SigninEvent;
        private readonly InputValidator _validator;

        public LoginWindow(BlitsMeClientAppContext appContext, LoginDetails details, AutoResetEvent signinEvent)
        {
            InitializeComponent();
            _appContext = appContext;
            _loginDetails = details;
            if(_loginDetails != null)
            {
                Username.Text = _loginDetails.username;
            }
            this.SigninEvent = signinEvent;
            _validator = new InputValidator(null, null);
        }

        public void Click_ForgotPassword(object sender, RequestNavigateEventArgs e)
        {
        }

        public void NewUserCreate(object sender, RequestNavigateEventArgs e)
        {
            Logger.Debug("Launching signupWindow");
            var signUpWindow = new SignUpWindow(_appContext) { Owner = this };
            try
            {
                signUpWindow.ShowDialog();
                Username.Text = signUpWindow.Username.Text;
                Password.Password = signUpWindow.Password.Password;
                ProcessSignin();
            }
            catch (Exception ex)
            {
                Logger.Error("SignupWindow failed : " + ex.Message, ex);
            }
        }

        public void LoginFailed()
        {
            Password.Password = "";
            _validator.ValidateFieldNonEmpty(Password, Password.Password, PasswordLabel, "");
        }

        private void signin_click(object sender, RoutedEventArgs e)
        {
            ProcessSignin();
        }

        private void ProcessSignin()
        {
            bool dataOK = true;
            ResetStatus();
            dataOK = _validator.ValidateFieldNonEmpty(Password, Password.Password, PasswordLabel, "") && dataOK;
            dataOK = _validator.ValidateFieldNonEmpty(Username, Username.Text, UsernameLabel, "") && dataOK;
            if (dataOK)
            {
                _loginDetails.username = Username.Text;
                _loginDetails.passwordHash = Util.getSingleton().hashPassword(Password.Password);
                Logger.Debug("Got username and password, notifying app");
                SigninEvent.Set();
            }
        }

        private void ResetStatus()
        {
            _validator.ResetStatus(new Control[] { Username, Password }, new[] { UsernameLabel, PasswordLabel });
        }

        private void WindowStateChanged(object sender, EventArgs e)
        {
            HideIfMinimized(sender, e);
        }

        private void HideIfMinimized(object sender, EventArgs e)
        {
            if (WindowState.Minimized == this.WindowState)
            {
                this.Hide();
            }
        }

        private void HideIfClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void username_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                Password.Focus();
            }
        }

        private void password_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                ProcessSignin();
            }
        }

        public void SignalLoggingIn()
        {
            if (Dispatcher.CheckAccess())
            {
                NewUser.IsEnabled = false;
                Username.IsEnabled = false;
                Password.IsEnabled = false;
                //ForgotPassword.IsEnabled = false;
                signin.Content = "Logging In";
                signin.IsEnabled = false;
            }
            else
            {
                Dispatcher.Invoke(new Action(SignalLoggingIn));
            }
        }

        public void SignalPleaseLogin()
        {
            if (Dispatcher.CheckAccess())
            {
                NewUser.IsEnabled = true;
                Username.IsEnabled = true;
                Password.IsEnabled = true;
                //ForgotPassword.IsEnabled = true;
                signin.Content = "Sign In";
                signin.IsEnabled = true;
            }
            else
            {
                Dispatcher.Invoke(new Action(SignalPleaseLogin));
            }
        }
    }
}