using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using BlitsMe.Agent.Components;
using BlitsMe.Agent.Components.Person;
using BlitsMe.Agent.UI.WPF.API;
using BlitsMe.Agent.UI.WPF.Utils;
using BlitsMe.Cloud.Exceptions;
using log4net;

namespace BlitsMe.Agent.UI.WPF.Engage
{
    /// <summary>
    /// Interaction logic for ContactSettings.xaml
    /// </summary>
    public partial class ContactSettings : BlitsMeModalUserControl
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ContactSettings));
        private readonly EngagementWindowDataContext _dataContext;
        private bool _updatedRelationship;
        //private UiHelper _uiHelper;

        internal ContactSettings(EngagementWindowDataContext dataContext)
        {
            this.InitializeComponent();
            InitBlitsMeModalUserControl(Disabler,null, ErrorText);
            _dataContext = dataContext;
            ProcessingWord = "Saving";
            //_uiHelper = new UiHelper(Dispatcher, Disabler, null, ErrorText);
            UnattendedAccessCheckbox.Click += CheckBox_Click;
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (UnattendedAccessCheckbox.IsChecked == true)
            {
                ThreadPool.QueueUserWorkItem(state => BlitsMeClientAppContext.CurrentAppContext.Alert(
                    "Please note, that by checking this box, you are allowing " +
                    _dataContext.Engagement.SecondParty.Person.Name +
                    " to access your desktop without prompting you for authorization. This is a potential security risk, please click OK to indicate you understand and accept the risks associated with this setting."));
            }
        }



        protected override bool CommitInput()
        {
            UiHelper.RunElevation("Saving", UpdateRelationship, "contact settings change");
            return _updatedRelationship;
        }



        private void UpdateRelationship(string tokenId, string securityKey)
        {
            try
            {
                var relationship = new Relationship
                {
                    IHaveUnattendedAccess = _dataContext.Engagement.SecondParty.Relationship.IHaveUnattendedAccess
                };
                Dispatcher.Invoke(new Action(() => { relationship.TheyHaveUnattendedAccess = (UnattendedAccessCheckbox.IsChecked == true); }));
                BlitsMeClientAppContext.CurrentAppContext.RosterManager.UpdateRelationship(
                    _dataContext.Engagement.SecondParty.Person.Username,
                    relationship, tokenId, securityKey);
                _updatedRelationship = true;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to update the relationship to " + _dataContext.Engagement.SecondParty.Person.Username + " : " + ex.Message, ex);
                UiHelper.Validator.SetError("Failed to save changes to server");
            }
        }

        protected override bool ValidateInput()
        {
            return true;
        }

        protected override void Show()
        {
            _dataContext.ContactSettingsEnabled = true;
        }

        protected override void Hide()
        {
            _dataContext.ContactSettingsEnabled = false;
        }

        protected override void ResetInputs()
        {
            _updatedRelationship = false;
            UnattendedAccessCheckbox.IsChecked = _dataContext.Engagement.SecondParty.Relationship.TheyHaveUnattendedAccess;
        }
    }
}