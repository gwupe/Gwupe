using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using BlitsMe.Agent.Components;
using BlitsMe.Agent.UI.WPF.Utils;
using BlitsMe.Cloud.Exceptions;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using log4net;
using MahApps.Metro.Controls;

namespace BlitsMe.Agent.UI.WPF
{
    /// <summary>
    /// Interaction logic for SignUpControl.xaml
    /// </summary>
    public partial class SignUpControl : UserControl
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SignUpControl));
        private readonly BlitsMeClientAppContext _appContext;
        private bool isSigningUp = false;
        private readonly InputValidator _validator;

        public SignUpControl()
        {
            _appContext = BlitsMeClientAppContext.CurrentAppContext;
            this.InitializeComponent();
            _validator = new InputValidator(null,ErrorText);
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
                    ConfirmPasswordWindow confirmPasswordWindow = new ConfirmPasswordWindow { Owner = _appContext.UIManager.CurrentWindow };
                    confirmPasswordWindow.ShowDialog();
                    if (!confirmPasswordWindow.Cancelled)
                    {
                        // OK, password will be changed
                        if (!confirmPasswordWindow.ConfirmPassword.Password.Equals(Password.Password))
                        {
                            Password.Background = new SolidColorBrush(Colors.MistyRose);
                            _validator.setError("Passwords don't match");
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
                    _validator.setError("Cannot sign up, BlitsMe is not connected.");
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
                    _validator.setError("Email address in use");
                    foundError = true;
                }
                if (dataSubmitErrorArgs.HasError(SignupRs.SignupErrorUserExists))
                {
                    Username.Background = new SolidColorBrush(Colors.MistyRose);
                    _validator.setError("Username already in use");
                    foundError = true;
                }
                if (dataSubmitErrorArgs.HasError(SignupRs.SignupErrorPasswordComplexity))
                {
                    Password.Background = new SolidColorBrush(Colors.MistyRose);
                    _validator.setError("Password is insecure");
                    foundError = true;
                }
                if (!foundError)
                {
                    _validator.setError("Unknown Error");
                }
            }
        }
            
        public void ShowLogin(object sender, RequestNavigateEventArgs e)
        {
            BlitsMeClientAppContext.CurrentAppContext.UIManager.PromptLogin();
        }
    }
}