using System;
using BlitsMe.Agent.UI.WPF.API;

namespace BlitsMe.Agent.UI.WPF
{
    /// <summary>
    /// Interaction logic for ConfirmPasswordControl.xaml
    /// </summary>
    public partial class ConfirmPasswordControl : BlitsMeModalUserControl
    {
        public ConfirmPasswordControl()
        {
            InitializeComponent();
            InitBlitsMeModalUserControl(Disabler,null,null);
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
