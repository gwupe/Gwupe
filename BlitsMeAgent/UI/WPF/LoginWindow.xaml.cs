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
        private LoginDetails loginDetails;
        public AutoResetEvent signinEvent;

		public LoginWindow(LoginDetails details, AutoResetEvent signinEvent)
		{
			this.InitializeComponent();
            this.loginDetails = details;
            this.signinEvent = signinEvent;
		}

        public void forgotPassword(object sender, RequestNavigateEventArgs e)
        {
        }

        public void newUserCreate(object sender, RequestNavigateEventArgs e)
        {
        }

        private void checkGuest(object sender, RoutedEventArgs e)
        {
            username.Background = new SolidColorBrush(Colors.White);
            username.Text = "guest";
            password.Password = "";
            username.IsEnabled = false;
            password.IsEnabled = false;
        }

        private void uncheckGuest(object sender, RoutedEventArgs e)
        {
            username.Background = new SolidColorBrush(Colors.White);
            username.IsEnabled = true;
            password.IsEnabled = true;
        }

        public void loginFailed()
        {
            passwordLabel.Foreground = new SolidColorBrush(Colors.Red);
            password.Background = new SolidColorBrush(Colors.MistyRose);
        }

        private void signin_click(object sender, RoutedEventArgs e)
        {
            processSignin();
        }

        private void processSignin() {
            bool fail = false;
            if (username.Text == null || username.Text.Equals(""))
            {
                username.Background = new SolidColorBrush(Colors.MistyRose);
                fail = true;
            }
            else
            {
                username.Background = new SolidColorBrush(Colors.White);
            }
            if ((password.Password == null || password.Password.Equals("")) && (username.Text == null || !username.Text.Equals("guest")))
            {
                password.Background = new SolidColorBrush(Colors.MistyRose);
                fail = true;
            }
            else
            {
                passwordLabel.Foreground = new SolidColorBrush(Colors.White);
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

        private void windowStateChanged(object sender, EventArgs e)
        {
            hideIfMinimized(sender, e);
        }

        private void hideIfMinimized(object sender, EventArgs e)
        {
            if (WindowState.Minimized == this.WindowState)
            {
                this.Hide();
            }
        }

        private void hideIfClosing(object sender, CancelEventArgs e)
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
                processSignin();
            }
        }

	}
}