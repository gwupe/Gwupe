using System;
using System.Windows;
using BlitsMe.Agent.UI.WPF.API;

namespace BlitsMe.Agent.UI.WPF
{
    /// <summary>
    /// Interaction logic for AlertControl.xaml
    /// </summary>
    public partial class AlertControl : BlitsMeModalUserControl
    {
        private readonly DashboardDataContext _dashboardDataContext;

        public AlertControl(DashboardDataContext dashboardDataContext)
        {
            InitializeComponent();
            _dashboardDataContext = dashboardDataContext;
            InitBlitsMeModalUserControl(Disabler, null, null);
        }

        protected override bool ValidateInput()
        {
            return true;
        }

        public void SetPrompt(String message)
        {
            Dispatcher.Invoke(new Action(() => AlertMessage.Text = message));
        }

        protected override void Show()
        {
            _dashboardDataContext.DashboardState = DashboardState.Alert;
        }

        protected override void Hide()
        {
            _dashboardDataContext.DashboardState = DashboardState.Default;
        }

        protected override void ResetInputs()
        {
        }

        protected override bool CommitInput()
        {
            return true;
        }
    }
}
