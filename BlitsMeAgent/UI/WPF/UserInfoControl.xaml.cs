using System;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Gwupe.Agent.Annotations;
using Gwupe.Agent.Managers;
using Gwupe.Agent.UI.WPF.API;
using Gwupe.Agent.UI.WPF.Utils;
using Gwupe.Cloud.Exceptions;
using Gwupe.Cloud.Messaging.API;
using Gwupe.Cloud.Messaging.Response;
using log4net;

namespace Gwupe.Agent.UI.WPF
{
    /// <summary>
    /// Interaction logic for UserInfo.xaml
    /// </summary>
    public partial class UserInfoControl : IDashboardContentControl, IGwupeUserControl
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(UserInfoControl));
        private readonly GwupeClientAppContext _appContext;
        private readonly UiHelper _uiHelper;

        public UserInfoControl(GwupeClientAppContext appContext)
        {
            this.InitializeComponent();
            _uiHelper = new UiHelper(Dispatcher, Disabler, StatusText, ErrorText);
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
            dataOK = _uiHelper.Validator.ValidateFieldNonEmpty(Email, Email.Text, EmailLabel, "Please enter your email address") && _uiHelper.Validator.ValidateEmail(Email, EmailLabel) && dataOK;
            dataOK = _uiHelper.Validator.ValidateFieldNonEmpty(Location, Location.Text, LocationLabel, "Please enter your location", "City, Country") && dataOK;
            if (PasswordChange != null && PasswordChange.IsChecked == true)
            {
                dataOK = _uiHelper.Validator.ValidateFieldNonEmpty(Password, Password.Password, PasswordLabel, "Please enter your password") && dataOK;
            }
            dataOK = _uiHelper.Validator.ValidateFieldNonEmpty(Lastname, Lastname.Text, NameLabel, "Please enter your last name", "Last") && dataOK;
            dataOK = _uiHelper.Validator.ValidateFieldNonEmpty(Firstname, Firstname.Text, NameLabel, "Please enter your first name", "First") && dataOK;

            if (dataOK)
            {
                if (_appContext.ConnectionManager.Connection.isEstablished())
                {
                    if (PasswordChange != null && PasswordChange.IsChecked == true)
                    {
                        var confirmPasswordWindow = new ConfirmPasswordWindow();
                        _appContext.UIManager.ShowDialog(confirmPasswordWindow);
                        if (!confirmPasswordWindow.Cancelled)
                        {
                            // OK, password will be changed
                            if (!confirmPasswordWindow.ConfirmPassword.Password.Equals(Password.Password))
                            {
                                Password.Background = new SolidColorBrush(Colors.MistyRose);
                                PasswordLabel.Foreground = new SolidColorBrush(Colors.Red);
                                _uiHelper.Validator.SetError("Cannot save changes, passwords don't match");
                                return;
                            }
                        }
                        else
                        {
                            return;
                        }
                    }
                    ThreadPool.QueueUserWorkItem(state => _uiHelper.RunElevation("Saving", SaveCurrentUser, "save user"));
                }
            }
        }

        private void SaveCurrentUser(string tokenId, string securityKey)
        {
            try
            {
                _appContext.CurrentUserManager.SaveCurrentUser(tokenId, securityKey, GetPasswordChange());
                _uiHelper.Validator.SetStatus("Saved changes to server.");
                Dispatcher.Invoke(new Action(() =>
                {
                    Password.Password = "";
                    PasswordChange.IsChecked = false;
                    Password.IsEnabled = false;
                }));
            }
            catch (MessageException<UpdateUserRs> ex)
            {
                Logger.Error("Attempt to update user failed : " + ex.Message, ex);
                if ("WILL_NOT_PROCESS_AUTH".Equals(ex.Response.error))
                {
                    _uiHelper.Validator.SetError("Incorrect password, please try again");
                }
                else
                {
                    _uiHelper.Validator.SetError("Failed to save changes to server");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to save the current user : " + ex.Message, ex);
                _uiHelper.Validator.SetError("Failed to save changes to server");
            }
        }

        private string GetPasswordChange()
        {
            if (Dispatcher.CheckAccess())
            {
                return (PasswordChange != null && PasswordChange.IsChecked == true) ? Password.Password : null;
            }
            else
            {
                String ret = null;
                Dispatcher.Invoke(new Action(() => { ret = GetPasswordChange(); }));
                return ret;
            }
        }

        private void CancelChanges_Click(object sender, RoutedEventArgs e)
        {
            ResetStatus();
            ThreadPool.QueueUserWorkItem(state => ReloadCurrentUser());
        }

        private void ReloadCurrentUser()
        {
            _uiHelper.Disabler.DisableInputs(true, "Reloading");
            try
            {
                _appContext.CurrentUserManager.ReloadCurrentUser();
                _uiHelper.Validator.SetStatus("Reloaded user details from server.");
            }
            catch (Exception exception)
            {
                Logger.Error("Failed to reload current user : " + exception.Message, exception);
                _uiHelper.Validator.SetError("Failed to reload user details.");
            }
            finally
            {
                _uiHelper.Disabler.DisableInputs(false);
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
            _uiHelper.Validator.ResetStatus(new Control[] { Email, Location, Password, Lastname, Firstname }, new[] { EmailLabel, LocationLabel, PasswordLabel, NameLabel, null });
        }

        private void AvatarImage_Click(object sender, RoutedEventArgs e)
        {

            var avatarWindow = new AvatarImageWindow(_appContext) { ProfileImage = ImageStreamReader.CreateBitmapImage(_appContext.CurrentUserManager.CurrentUser.Avatar) };
            ApplyBlurEffect(UserControl);
            _appContext.UIManager.ShowDialog(avatarWindow);
            ClearBlurEffect(UserControl);
        }

        private void ClearBlurEffect(UserInfoControl userControl)
        {
            userControl.Background = new SolidColorBrush(Colors.Transparent);
            userControl.UserControl.Opacity = 100;
        }

        private void ApplyBlurEffect(UserInfoControl userControl)
        {
            userControl.Background = new SolidColorBrush(Colors.Gray);
            userControl.UserControl.Opacity = 0.4;
        }

        public void SetAsMain(Dashboard dashboard)
        {

        }

    }
}