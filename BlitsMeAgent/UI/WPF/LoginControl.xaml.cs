using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using Gwupe.Agent.Components;
using Gwupe.Agent.UI.WPF.Utils;
using log4net;
using System.Windows.Media;

namespace Gwupe.Agent.UI.WPF
{
    /// <summary>
    /// Interaction logic for LoginControl.xaml
    /// </summary>
    public partial class LoginControl : UserControl
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LoginControl));
        private readonly InputValidator _validator;
        private Dashboard _dashboard;
        public LoginControl(Dashboard dashboard)
        {
            this.InitializeComponent();
            _dashboard = dashboard;
            ApplyBlurEffect(dashboard);
            Username.Text = GwupeClientAppContext.CurrentAppContext.LoginManager.LoginDetails.Username ?? "";
            _validator = new InputValidator(null, null, Dispatcher);
        }

        private void username_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                Password.Focus();
            }
        }

        public bool LoginFailed(DataSubmitErrorArgs dataSubmissionErrors)
        {
            if (dataSubmissionErrors.HasErrorField("PasswordHash"))
            {
                Password.Password = "";
                Validate();
                return true;
            }
            else if (dataSubmissionErrors.HasError(DataSubmitErrorCode.DataIncomplete))
            {
                Password.Password = "";
                Username.Text = "";
                Validate();
                return true;
            }
            return false;
        }

        private void password_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                ProcessLogin();
            }
        }

        private void ProcessLogin()
        {
            if (Validate())
            {
                ClearBlurEffect(_dashboard);
                Logger.Debug("Got username and password, submitting to login manager");
                GwupeClientAppContext.CurrentAppContext.LoginManager.Login(Username.Text, Password.Password);
            }
        }

        private bool Validate()
        {
            bool dataOK = true;
            ResetStatus();
            dataOK = _validator.ValidateFieldNonEmpty(Password, Password.Password, null, "") && dataOK;
            dataOK = _validator.ValidateFieldNonEmpty(Username, Username.Text, null, "") && dataOK;
            return dataOK;
        }

        private void ResetStatus()
        {
            _validator.ResetStatus(new Control[] { Username, Password }, new Label[] { null, null });
        }

        private void signin_click(object sender, System.Windows.RoutedEventArgs e)
        {
            ProcessLogin();
        }

        public void NewUserCreate(object sender, RequestNavigateEventArgs e)
        {
            GwupeClientAppContext.CurrentAppContext.UIManager.PromptSignup();
        }

        public void LoginGuestClick(object sender, RequestNavigateEventArgs e)
        {
            ResetStatus();
            ClearBlurEffect(_dashboard);
            Logger.Debug("Logging in as Guest");
            GwupeClientAppContext.CurrentAppContext.LoginManager.LoginGuest();
        }

        private void ApplyBlurEffect(Dashboard dashboard)
        {
            dashboard.Opacity = 0.9;
        }

        private void ClearBlurEffect(Dashboard dashboard)
        {
            dashboard.Opacity = 100;
        }
    }
}