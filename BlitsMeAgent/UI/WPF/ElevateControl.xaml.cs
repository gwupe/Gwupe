using System;
using System.Windows.Controls;
using BlitsMe.Agent.UI.WPF.API;

namespace BlitsMe.Agent.UI.WPF
{
    /// <summary>
    /// Interaction logic for ElevateControl.xaml
    /// </summary>
    public partial class ElevateControl : BlitsMeModalUserControl
    {
        private readonly DashboardDataContext _dashboardDataContext;

        public ElevateControl(DashboardDataContext dashboardDataContext)
        {
            this.InitializeComponent();
            _dashboardDataContext = dashboardDataContext;
            InitBlitsMeModalUserControl(Disabler, null, null);
            ProcessingWord = "Verifying";
            StartWithFocus = ConfirmPassword;
        }

        protected override void ResetInputs()
        {
            ConfirmPassword.Password = "";
            UiHelper.Validator.ResetStatus(new Control[] { ConfirmPassword });
        }

        protected override bool CommitInput()
        {
            return true;
        }

        protected override bool ValidateInput()
        {
            bool dataOK = true;
            dataOK = UiHelper.Validator.ValidateFieldNonEmpty(ConfirmPassword, ConfirmPassword.Password, null, "Please enter your password") && dataOK;
            return dataOK;
        }

        protected override void Show()
        {
            _dashboardDataContext.DashboardStateManager.EnableDashboardState(DashboardState.Elevate);
        }

        protected override void Hide()
        {
            _dashboardDataContext.DashboardStateManager.DisableDashboardState(DashboardState.Elevate);
        }

        public void SetPrompt(string message)
        {
            if (!Disabler.CheckAccess())
            {
                Dispatcher.Invoke(new Action(() => SetPrompt(message)));
                return;
            }
            Message.Text = message;
        }
    }
}