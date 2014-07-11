using System;
using System.Windows.Controls;
using BlitsMe.Agent.UI.WPF.API;

namespace BlitsMe.Agent.UI.WPF
{
    /// <summary>
    /// Interaction logic for ElevateControl.xaml
    /// </summary>
    public partial class FaultReportControl : BlitsMeModalUserControl
    {
        private readonly DashboardDataContext _dashboardDataContext;

        public FaultReportControl(DashboardDataContext dashboardDataContext)
        {
            this.InitializeComponent();
            _dashboardDataContext = dashboardDataContext;
            InitBlitsMeModalUserControl(Disabler, null, null);
            ProcessingWord = "Submitting";
            StartWithFocus = UserReport;
        }

        public FaultReport FaultReport { get; set; }

        protected override void ResetInputs()
        {
            UserReport.Text = "";
            UiHelper.Validator.ResetStatus(new Control[] { UserReport });
        }

        protected override bool CommitInput()
        {
            return true;
        }

        protected override bool ValidateInput()
        {
            bool dataOK = true;
            dataOK = UiHelper.Validator.ValidateFieldNonEmpty(UserReport, UserReport.Text, null, "Please tell us what problem you experienced.") && dataOK;
            return dataOK;
        }

        protected override void Show()
        {
            _dashboardDataContext.DashboardState = DashboardState.UserInputPrompt;
        }

        protected override void Hide()
        {
            _dashboardDataContext.DashboardState = DashboardState.Default;
        }
    }
}