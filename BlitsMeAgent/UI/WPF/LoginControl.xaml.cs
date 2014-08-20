using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using BlitsMe.Agent.UI.WPF.Utils;
using log4net;
using System.Windows.Media;

namespace BlitsMe.Agent.UI.WPF
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
            Username.Text = BlitsMeClientAppContext.CurrentAppContext.LoginManager.LoginDetails.Username ?? "";
            _validator = new InputValidator(null, null, Dispatcher);
        }

        private void username_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                Password.Focus();
            }
        }

        public void LoginFailed()
        {
            if (!Dispatcher.CheckAccess())
                Dispatcher.Invoke(new Action(LoginFailed));
            else
            {
                Password.Password = "";
                _validator.ValidateFieldNonEmpty(Password, Password.Password, null, "");
            }
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
            bool dataOK = true;
            ResetStatus();
            dataOK = _validator.ValidateFieldNonEmpty(Password, Password.Password, null, "") && dataOK;
            dataOK = _validator.ValidateFieldNonEmpty(Username, Username.Text, null, "") && dataOK;
            if (dataOK)
            {
                ClearBlurEffect(_dashboard);
                Logger.Debug("Got username and password, submitting to login manager");
                BlitsMeClientAppContext.CurrentAppContext.LoginManager.Login(Username.Text, Password.Password);
                _dashboard.DashboardData.DashboardStateManager.DisableDashboardState(DashboardState.Login);
            }
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
            BlitsMeClientAppContext.CurrentAppContext.UIManager.PromptSignup();
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