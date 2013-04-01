using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BlitsMe.Cloud.Exceptions;
using BlitsMe.Cloud.Messaging.API;
using BlitsMe.Cloud.Messaging.Response;
using log4net;

namespace BlitsMe.Agent.UI.WPF
{
    /// <summary>
    /// Interaction logic for UserInfo.xaml
    /// </summary>
    public partial class UserInfoWindow : UserControl
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(UserInfoWindow));
        private readonly BlitsMeClientAppContext _appContext;

        public UserInfoWindow(BlitsMeClientAppContext appContext)
        {
            this.InitializeComponent();
            _appContext = appContext;
            DataContext = _appContext.CurrentUserManager.CurrentUser;
            _appContext.CurrentUserManager.CurrentUserChanged += CurrentUserManagerOnCurrentUserChanged;
        }

        private void CurrentUserManagerOnCurrentUserChanged(object sender, EventArgs eventArgs)
        {
            if (Dispatcher.CheckAccess())
            {
                DataContext = _appContext.CurrentUserManager.CurrentUser;
            }
            else
            {
                Dispatcher.Invoke(new Action(() => CurrentUserManagerOnCurrentUserChanged(sender, eventArgs)));
            }
        }

        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            resetError();

            bool dataOK = true;
            dataOK = ValidateFieldNonEmpty(Email, Email.Text, EmailLabel, "Please enter your email address") && ValidateEmail() && dataOK;
            dataOK = ValidateFieldNonEmpty(Location, Location.Text, LocationLabel, "Please enter your location", "City, Country") && dataOK;
            if (PasswordChange != null && (bool)PasswordChange.IsChecked)
            {
                dataOK = ValidateFieldNonEmpty(Password, Password.Password, PasswordLabel, "Please enter your password") && dataOK;
            }
            dataOK = ValidateFieldNonEmpty(Lastname, Lastname.Text, NameLabel, "Please enter your last name", "Last") && dataOK;
            dataOK = ValidateFieldNonEmpty(Firstname, Firstname.Text, NameLabel, "Please enter your first name", "First") && dataOK;

            if (dataOK)
            {
                if (_appContext.ConnectionManager.Connection.isEstablished())
                {
                    if (PasswordChange != null && (bool)PasswordChange.IsChecked)
                    {
                        ConfirmPasswordWindow confirmPasswordWindow = new ConfirmPasswordWindow { Owner = _appContext.UIDashBoard };
                        confirmPasswordWindow.ShowDialog();
                        if (!confirmPasswordWindow.Cancelled)
                        {
                            // OK, password will be changed
                            if (!confirmPasswordWindow.ConfirmPassword.Password.Equals(Password.Password))
                            {
                                Password.Background = new SolidColorBrush(Colors.MistyRose);
                                PasswordLabel.Foreground = new SolidColorBrush(Colors.Red);
                                setError("Cannot save changes, passwords don't match");
                                return;
                            }
                        }
                        else
                        {
                            return;
                        }
                    }
                    try
                    {
                        String tokenId;
                        String securityKey;
                        if (_appContext.Elevate(_appContext.UIDashBoard, out tokenId, out securityKey))
                        {

                            try
                            {
                                _appContext.CurrentUserManager.SaveCurrentUser(tokenId, securityKey, (PasswordChange != null && (bool)PasswordChange.IsChecked) ? Password.Password : null);
                                StatusText.Text = "Your changes have been saved";
                                StatusText.Visibility = Visibility.Visible;
                                Password.Password = "";
                                PasswordChange.IsChecked = false;
                                Password.IsEnabled = false;
                            }
                            catch (MessageException<UpdateUserRs> ex)
                            {
                                Logger.Error("Attempt to update user failed : " + ex.Message, ex);
                                if ("WILL_NOT_PROCESS_AUTH".Equals(ex.Response.error))
                                {
                                    setError("Incorrect password, please try again");
                                }
                                else
                                {
                                    setError("Failed to save changes to server");
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Failed to save the current user : " + ex.Message, ex);
                                setError("Failed to save changes to server");
                            }
                        }
                        else
                        {
                            setError("Failed to authorise user details change");
                        }
                    }
                    catch(Exception ex)
                    {
                        Logger.Error("Failed to elevate privileges for user details change : " + ex.Message,ex);
                        setError("Failed to elevate privileges to change details");
                    }
                }
            }
        }

        private void CancelChanges_Click(object sender, RoutedEventArgs e)
        {
            resetError();
            try
            {
                _appContext.CurrentUserManager.ReloadCurrentUser();
            }
            catch (Exception exception)
            {
                Logger.Error("Failed to reload current user : " + exception.Message, exception);
            }
        }

        private void PasswordChange_Click(object sender, RoutedEventArgs e)
        {
            if (PasswordChange.IsChecked != null && (bool)PasswordChange.IsChecked)
            {
                Password.IsEnabled = true;
            }
            else
            {
                Password.Password = "";
                Password.IsEnabled = false;
            }
        }

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
            StatusText.Visibility = Visibility.Hidden;
            ErrorText.Text = errorText;
            ErrorText.Visibility = Visibility.Visible;
        }

        private void resetError()
        {
            StatusText.Visibility = Visibility.Hidden;
            Email.Background = new SolidColorBrush(Colors.White);
            EmailLabel.Foreground = new SolidColorBrush(Colors.Black);
            Username.Background = new SolidColorBrush(Colors.White);
            UsernameLabel.Foreground = new SolidColorBrush(Colors.Black);
            Password.Background = new SolidColorBrush(Colors.White);
            PasswordLabel.Foreground = new SolidColorBrush(Colors.Black);
            Location.Background = new SolidColorBrush(Colors.White);
            LocationLabel.Foreground = new SolidColorBrush(Colors.Black);
            Firstname.Background = new SolidColorBrush(Colors.White);
            Lastname.Background = new SolidColorBrush(Colors.White);
            NameLabel.Foreground = new SolidColorBrush(Colors.Black);
            ErrorText.Visibility = Visibility.Hidden;
        }
    }
}