using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlitsMe.Agent.Managers;
using BlitsMe.Agent.UI.WPF.API;
using BlitsMe.Agent.UI.WPF.Utils;
using BlitsMe.Cloud.Exceptions;
using BlitsMe.Cloud.Messaging.API;
using BlitsMe.Cloud.Messaging.Response;
using log4net;

namespace BlitsMe.Agent.UI.WPF
{
    /// <summary>
    /// Interaction logic for UserInfo.xaml
    /// </summary>
    public partial class UserInfoWindow : UserControl, IDashboardContentControl
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(UserInfoWindow));
        private readonly BlitsMeClientAppContext _appContext;
        private InputValidator validator;

        public UserInfoWindow(BlitsMeClientAppContext appContext)
        {
            this.InitializeComponent();
            validator = new InputValidator(StatusText, ErrorText);
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
            ResetStatus();

            bool dataOK = true;
            dataOK = validator.ValidateFieldNonEmpty(Email, Email.Text, EmailLabel, "Please enter your email address") && validator.ValidateEmail(Email, EmailLabel) && dataOK;
            dataOK = validator.ValidateFieldNonEmpty(Location, Location.Text, LocationLabel, "Please enter your location", "City, Country") && dataOK;
            if (PasswordChange != null && (bool)PasswordChange.IsChecked)
            {
                dataOK = validator.ValidateFieldNonEmpty(Password, Password.Password, PasswordLabel, "Please enter your password") && dataOK;
            }
            dataOK = validator.ValidateFieldNonEmpty(Lastname, Lastname.Text, NameLabel, "Please enter your last name", "Last") && dataOK;
            dataOK = validator.ValidateFieldNonEmpty(Firstname, Firstname.Text, NameLabel, "Please enter your first name", "First") && dataOK;

            if (dataOK)
            {
                if (_appContext.ConnectionManager.Connection.isEstablished())
                {
                    if (PasswordChange != null && (bool)PasswordChange.IsChecked)
                    {
                        ConfirmPasswordWindow confirmPasswordWindow = new ConfirmPasswordWindow();
                        _appContext.UIManager.ShowDialog(confirmPasswordWindow);
                        if (!confirmPasswordWindow.Cancelled)
                        {
                            // OK, password will be changed
                            if (!confirmPasswordWindow.ConfirmPassword.Password.Equals(Password.Password))
                            {
                                Password.Background = new SolidColorBrush(Colors.MistyRose);
                                PasswordLabel.Foreground = new SolidColorBrush(Colors.Red);
                                validator.setError("Cannot save changes, passwords don't match");
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
                        if (_appContext.Elevate(_appContext.UIManager.CurrentWindow, out tokenId, out securityKey))
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
                                    validator.setError("Incorrect password, please try again");
                                }
                                else
                                {
                                    validator.setError("Failed to save changes to server");
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Failed to save the current user : " + ex.Message, ex);
                                validator.setError("Failed to save changes to server");
                            }
                        }
                        else
                        {
                            validator.setError("Failed to authorise user details change");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Failed to elevate privileges for user details change : " + ex.Message, ex);
                        validator.setError("Failed to elevate privileges to change details");
                    }
                }
            }
        }

        private void CancelChanges_Click(object sender, RoutedEventArgs e)
        {
            ResetStatus();
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

        private void ResetStatus()
        {
            validator.ResetStatus(new Control[] { Email, Location, Password, Lastname, Firstname }, new[] { EmailLabel, LocationLabel, PasswordLabel, NameLabel, null });
        }

        private void AvatarImage_Click(object sender, RoutedEventArgs e)
        {
            var avatarWindow = new AvatarImageWindow(_appContext) { ProfileImage = ImageStreamReader.CreateBitmapImage(_appContext.CurrentUserManager.CurrentUser.Avatar) };
            _appContext.UIManager.ShowDialog(avatarWindow);
        }

        public void SetAsMain(Dashboard dashboard)
        {

        }
    }
}