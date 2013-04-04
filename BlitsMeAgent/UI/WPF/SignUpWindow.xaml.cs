using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BlitsMe.Agent.UI.WPF.Utils;
using BlitsMe.Cloud.Exceptions;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using log4net;

namespace BlitsMe.Agent.UI.WPF
{
    /// <summary>
    /// Interaction logic for SignUpWindow.xaml
    /// </summary>
    public partial class SignUpWindow : Window
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SignUpWindow));
        private readonly BlitsMeClientAppContext _appContext;
        private bool isSigningUp = false;
        private InputValidator validator;

        public SignUpWindow(BlitsMeClientAppContext appContext)
        {
            _appContext = appContext;
            this.InitializeComponent();
            validator = new InputValidator(StatusText,ErrorText);

            // Insert code required on object creation below this point.
        }

        private void username_KeyDown(object sender, KeyEventArgs e)
        {
        }

        private void password_KeyDown(object sender, KeyEventArgs e)
        {
        }

        private void signin_click(object sender, RoutedEventArgs e)
        {
            ResetStatus();

            bool dataOK = true;
            dataOK = validator.ValidateFieldNonEmpty(Email, Email.Text, EmailLabel, "Please enter your email address") && validator.ValidateEmail(Email,EmailLabel) && dataOK;
            dataOK = validator.ValidateFieldNonEmpty(Location, Location.Text, LocationLabel, "Please enter your location", "City, Country") && dataOK;
            dataOK = validator.ValidateFieldNonEmpty(Password, Password.Password, PasswordLabel, "Please enter your password") && dataOK;
            dataOK = validator.ValidateFieldNonEmpty(Username, Username.Text, UsernameLabel, "Please enter your preferred username") && dataOK;
            dataOK = validator.ValidateFieldNonEmpty(Lastname, Lastname.Text, NameLabel, "Please enter your last name", "Last") && dataOK;
            dataOK = validator.ValidateFieldNonEmpty(Firstname, Firstname.Text, NameLabel, "Please enter your first name", "First") && dataOK;

            if (dataOK)
            {
                if (_appContext.ConnectionManager.Connection.isEstablished())
                {
                    ConfirmPasswordWindow confirmPasswordWindow = new ConfirmPasswordWindow { Owner = this };
                    confirmPasswordWindow.ShowDialog();
                    if (!confirmPasswordWindow.Cancelled)
                    {
                        // OK, password will be changed
                        if (!confirmPasswordWindow.ConfirmPassword.Password.Equals(Password.Password))
                        {
                            Password.Background = new SolidColorBrush(Colors.MistyRose);
                            PasswordLabel.Foreground = new SolidColorBrush(Colors.Red);
                            validator.setError("Passwords don't match");
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                    var request = new SignupRq
                        {
                            email = Email.Text.Trim(),
                            location = Location.Text.Trim(),
                            password = Password.Password,
                            username = Username.Text.Trim(),
                            firstname = Firstname.Text.Trim(),
                            lastname = Lastname.Text.Trim(),
                            supporter = Supporter.IsChecked != null && (bool) Supporter.IsChecked
                        };
                    try
                    {
                        SignupRs response = _appContext.ConnectionManager.Connection.Request<SignupRq, SignupRs>(request);
                        Close();
                    } catch (MessageException<SignupRs> ex)
                    {
                        Logger.Warn("Failed to signup : " + ex.Message);
                        validator.setError(ex.Response.errorMessage);
                        if (ex.Response.error.Equals(SignupRs.SignupErrorEmailAddressInUse))
                        {
                            Email.Background = new SolidColorBrush(Colors.MistyRose);
                            EmailLabel.Foreground = new SolidColorBrush(Colors.Red);
                        }
                        else if (ex.Response.error.Equals(SignupRs.SignupErrorUsernameTaken))
                        {
                            Username.Background = new SolidColorBrush(Colors.MistyRose);
                            UsernameLabel.Foreground = new SolidColorBrush(Colors.Red);
                        }
                        else if (ex.Response.error.Equals(SignupRs.SignupErrorPasswordComplexity))
                        {
                            Password.Background = new SolidColorBrush(Colors.MistyRose);
                            PasswordLabel.Foreground = new SolidColorBrush(Colors.Red);
                        }
                    } catch (Exception ex)
                    {
                        Logger.Warn("Failed to signup : " + ex.Message);
                        validator.setError("An unknown error occured during signup");
                    }
                }
                else
                {
                    validator.setError("Cannot sign up, BlitsMe is not connected.");
                }
            }
        }

        private void ResetStatus()
        {
            validator.ResetStatus(new Control[] {Email,Username,Password,Location,Firstname,Lastname}, new[] {EmailLabel,UsernameLabel,PasswordLabel,LocationLabel,NameLabel,null} );
        }
/*
        private bool ValidateEmail()
        {
            bool dataOK = true;
            if (!Regex.IsMatch(Email.Text.Trim(),
                               @"^(?("")(""[^""]+?""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                               @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9]{2,17}))$",
                               RegexOptions.IgnoreCase))
            {
                setError("Please enter a valid email address");
                Email.Background = new SolidColorBrush(Colors.MistyRose);
                EmailLabel.Foreground = new SolidColorBrush(Colors.Red);
                dataOK = false;
            }
            ;
            return dataOK;
        }

        private bool ValidateFieldNonEmpty(Control textBox, string text, Label textLabel, string errorText, string defaultValue = "")
        {
            if (text.Equals(defaultValue) || String.IsNullOrWhiteSpace(text))
            {
                textBox.Background = new SolidColorBrush(Colors.MistyRose);
                textLabel.Foreground = new SolidColorBrush(Colors.Red);
                setError(errorText);
                return false;
            }
            return true;
        }

        private void setError(string errorText)
        {
            ErrorText.Text = errorText;
            ErrorText.Visibility = Visibility.Visible;
        }
*/
        private void location_GotFocus(object sender, RoutedEventArgs e)
        {
            if (Location.Text.Equals("City, Country"))
            {
                Location.Text = "";
            }
        }

        private void location_LostFocus(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(Location.Text))
            {
                Location.Text = "City, Country";
            }
        }

        private void Firstname_GotFocus(object sender, RoutedEventArgs e)
        {
            if (Firstname.Text.Equals("First"))
            {
                Firstname.Text = "";
            }
        }

        private void Firstname_LostFocus(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(Firstname.Text))
            {
                Firstname.Text = "First";
            }
        }

        private void Lastname_GotFocus(object sender, RoutedEventArgs e)
        {
            if (Lastname.Text.Equals("Last"))
            {
                Lastname.Text = "";
            }
        }

        private void Lastname_LostFocus(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(Lastname.Text))
            {
                Lastname.Text = "Last";
            }
        }
    }
}