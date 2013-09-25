using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using BlitsMe.Agent.UI.WPF.Utils;
using log4net;

namespace BlitsMe.Agent.UI.WPF
{
	/// <summary>
	/// Interaction logic for LoginControl.xaml
	/// </summary>
	public partial class LoginControl : UserControl
	{
	    private static readonly ILog Logger = LogManager.GetLogger(typeof (LoginControl));
        private readonly InputValidator _validator;
		public LoginControl()
		{
			this.InitializeComponent();
		    Username.Text = BlitsMeClientAppContext.CurrentAppContext.LoginManager.LoginDetails.Username ?? "";
            _validator = new InputValidator(null, null);
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
            Password.Password = "";
            _validator.ValidateFieldNonEmpty(Password, Password.Password, null, "");
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
                Logger.Debug("Got username and password, submitting to login manager");
                BlitsMeClientAppContext.CurrentAppContext.LoginManager.Login(Username.Text, Password.Password);
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
	    }
	}
}