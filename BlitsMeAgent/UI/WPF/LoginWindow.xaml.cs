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
using BlitsMe.Common.Security;
using log4net;

namespace BlitsMe.Agent.UI.WPF
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(LoginWindow));
        private readonly BlitsMeClientAppContext _appContext;
        private LoginDetails loginDetails;
        public AutoResetEvent signinEvent;

        public LoginWindow(BlitsMeClientAppContext appContext, LoginDetails details, AutoResetEvent signinEvent)
        {
            InitializeComponent();
            _appContext = appContext;
            loginDetails = details;
            if(loginDetails != null)
            {
                username.Text = loginDetails.username;
            }
            this.signinEvent = signinEvent;
        }

        public void Click_ForgotPassword(object sender, RequestNavigateEventArgs e)
        {
        }

        public void NewUserCreate(object sender, RequestNavigateEventArgs e)
        {
            logger.Debug("Launching signupWindow");
            var signUpWindow = new SignUpWindow(_appContext) { Owner = this };
            try
            {
                signUpWindow.ShowDialog();
                username.Text = signUpWindow.Username.Text;
                password.Password = signUpWindow.Password.Password;
                ProcessSignin();
            }
            catch (Exception ex)
            {
                logger.Error("SignupWindow failed : " + ex.Message, ex);
            }
        }

        public void LoginFailed()
        {
            passwordLabel.Foreground = new SolidColorBrush(Colors.Red);
            password.Background = new SolidColorBrush(Colors.MistyRose);
            password.Password = "";
        }

        private void signin_click(object sender, RoutedEventArgs e)
        {
            ProcessSignin();
        }

        private void ProcessSignin()
        {
            bool fail = false;
            if (username.Text == null || username.Text.Equals(""))
            {
                username.Background = new SolidColorBrush(Colors.MistyRose);
                username.Focus();
                fail = true;
            }
            else
            {
                username.Background = new SolidColorBrush(Colors.White);
            }
            if ((password.Password == null || password.Password.Equals("")) && (username.Text == null || !username.Text.Equals("guest")))
            {
                password.Background = new SolidColorBrush(Colors.MistyRose);
                if(!username.IsFocused) password.Focus();
                fail = true;
            }
            else
            {
                passwordLabel.Foreground = new SolidColorBrush(Colors.Black);
                password.Background = new SolidColorBrush(Colors.White);
            }
            if (!fail)
            {
                loginDetails.username = username.Text;
                loginDetails.passwordHash = Util.getSingleton().hashPassword(password.Password);
                logger.Debug("Got username and password, notifying app");
                signinEvent.Set();
            }
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
                password.Focus();
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
                username.IsEnabled = false;
                password.IsEnabled = false;
                ForgotPassword.IsEnabled = false;
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
                username.IsEnabled = true;
                password.IsEnabled = true;
                ForgotPassword.IsEnabled = true;
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