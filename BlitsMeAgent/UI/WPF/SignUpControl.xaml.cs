using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using Gwupe.Agent.Components;
using Gwupe.Agent.UI.WPF.Utils;
using Gwupe.Cloud.Exceptions;
using Gwupe.Cloud.Messaging.Request;
using Gwupe.Cloud.Messaging.Response;
using log4net;
using MahApps.Metro.Controls;

namespace Gwupe.Agent.UI.WPF
{
    /// <summary>
    /// Interaction logic for SignUpControl.xaml
    /// </summary>
    public partial class SignUpControl : UserControl
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SignUpControl));
        private readonly GwupeClientAppContext _appContext;
        private bool isSigningUp = false;
        private readonly InputValidator _validator;
        private Dashboard _dashboard;

        public SignUpControl(Dashboard dashboard)
        {
            _dashboard = dashboard;
            _appContext = GwupeClientAppContext.CurrentAppContext;
            this.InitializeComponent();
            _validator = new InputValidator(null,ErrorText,Dispatcher);
        }

        private void signin_click(object sender, RoutedEventArgs e)
        {
            ResetStatus();

            bool dataOK = true;
            dataOK = _validator.ValidateFieldMatches(Username, Username.Text, null,
                                                    "Username can only use normal characters", "", ".*[^a-zA-Z0-9_\\-\\.].*") && dataOK;
            dataOK = _validator.ValidateFieldMatches(Username, Username.Text, null,
                                                    "Username cannot contain spaces", "", ".* .*") && dataOK;
            dataOK = _validator.ValidateFieldNonEmpty(Email, Email.Text, null, "Please enter your email address") && _validator.ValidateEmail(Email, null) && dataOK;
            dataOK = _validator.ValidateFieldNonEmpty(Location, Location.Text, null, "Please enter your location", "City, Country") && dataOK;
            dataOK = _validator.ValidateFieldNonEmpty(Password, Password.Password, null, "Please enter your password") && dataOK;
            dataOK = _validator.ValidateFieldNonEmpty(Username, Username.Text, null, "Please enter your preferred username") && dataOK;
            dataOK = _validator.ValidateFieldNonEmpty(Lastname, Lastname.Text, null, "Please enter your last name", "Last") && dataOK;
            dataOK = _validator.ValidateFieldNonEmpty(Firstname, Firstname.Text, null, "Please enter your first name", "First") && dataOK;


            if (dataOK)
            {
                if (_appContext.ConnectionManager.Connection.isEstablished())
                {
                    ClearBlurEffect(_dashboard);
                    ConfirmPasswordWindow confirmPasswordWindow = new ConfirmPasswordWindow();
                    GwupeClientAppContext.CurrentAppContext.UIManager.ShowDialog(confirmPasswordWindow);
                    if (!confirmPasswordWindow.Cancelled)
                    {
                        // OK, password will be changed
                        if (!confirmPasswordWindow.ConfirmPassword.Password.Equals(Password.Password))
                        {
                            Password.Background = new SolidColorBrush(Colors.MistyRose);
                            _validator.SetError("Passwords don't match");
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                    _appContext.LoginManager.Signup(Firstname.Text, Lastname.Text, Username.Text, Password.Password, Email.Text, Location.Text, Supporter.IsChecked != null && (bool)Supporter.IsChecked);
                }
                else
                {
                    _validator.SetError("Cannot sign up, Gwupe is not connected.");
                }
            }
        }

        private void ResetStatus()
        {
            _validator.ResetStatus(new Control[] {Email,Username,Password,Location,Firstname,Lastname}, new Label[] {null,null,null,null,null,null} );
        }

        internal void SetErrors(DataSubmitErrorArgs dataSubmitErrorArgs)
        {
            if (!Dispatcher.CheckAccess())
                Dispatcher.Invoke(new Action(() => SetErrors(dataSubmitErrorArgs)));
            else
            {
                ResetStatus();
                bool foundError = false;
                if (dataSubmitErrorArgs.HasError(SignupRs.SignupErrorEmailAddressInUse))
                {
                    Email.Background = new SolidColorBrush(Colors.MistyRose);
                    _validator.SetError("Email address in use");
                    foundError = true;
                }
                if (dataSubmitErrorArgs.HasError(SignupRs.SignupErrorUserExists))
                {
                    Username.Background = new SolidColorBrush(Colors.MistyRose);
                    _validator.SetError("Username already in use");
                    foundError = true;
                }
                if (dataSubmitErrorArgs.HasError(SignupRs.SignupErrorPasswordComplexity))
                {
                    Password.Background = new SolidColorBrush(Colors.MistyRose);
                    _validator.SetError("Password is insecure");
                    foundError = true;
                }
                if (!foundError)
                {
                    _validator.SetError("Unknown Error");
                }
            }
        }

        private void cancel_click(object sender, RoutedEventArgs e)
        {
            if (_appContext.LoginManager.IsLoggedIn)
            {
                GwupeClientAppContext.CurrentAppContext.UIManager.ClearDashboardState();
            }
            else
            {
                GwupeClientAppContext.CurrentAppContext.UIManager.PromptLogin();
            }
        }

        private void ClearBlurEffect(Dashboard dashboard)
        {
            dashboard.Opacity = 100;
        }
    }
}