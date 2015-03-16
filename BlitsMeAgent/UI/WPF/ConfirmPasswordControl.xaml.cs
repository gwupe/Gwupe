using System;
using Gwupe.Agent.UI.WPF.API;

namespace Gwupe.Agent.UI.WPF
{
    /// <summary>
    /// Interaction logic for ConfirmPasswordControl.xaml
    /// </summary>
    public partial class ConfirmPasswordControl : GwupeModalUserControl
    {
        public ConfirmPasswordControl()
        {
            InitializeComponent();
            InitGwupeModalUserControl(Disabler,null,null);
            StartWithFocus = ConfirmPassword;
        }

        protected override bool ValidateInput()
        {
            bool dataOK = true;
            dataOK = UiHelper.Validator.ValidateFieldNonEmpty(ConfirmPassword, ConfirmPassword.Password, null, "Please enter your password") && dataOK;
            return dataOK;
        }

        protected override void Show()
        {
            throw new NotImplementedException();
        }

        protected override void Hide()
        {
            throw new NotImplementedException();
        }

        protected override void ResetInputs()
        {
            throw new NotImplementedException();
        }

        protected override bool CommitInput()
        {
            return true;
        }
    }
}
