using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Gwupe.Agent.Components;
using Gwupe.Agent.Exceptions;
using Gwupe.Agent.UI.WPF.API;
using Gwupe.Cloud.Messaging.Elements;
using Gwupe.Cloud.Messaging.Request;
using Gwupe.Cloud.Messaging.Response;
using log4net;

namespace Gwupe.Agent.UI.WPF.Team
{
    /// <summary>
    /// Interaction logic for TeamSettingsControl.xaml
    /// </summary>
    public partial class TeamSettingsControl : GwupeDataCaptureForm
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TeamSettingsControl));
        private Components.Person.Team _team;

        public Components.Person.Team Team
        {
            get { return _team; }
            set
            {
                _team = value;
                DataContext = Team;
                ResetStatus();
            }
        }

        public TeamSettingsControl(ContentPresenter disabler)
        {
            InitializeComponent();
            InitGwupeDataCaptureForm(disabler, StatusText, ErrorText);
            ProcessingWord = "Updating";
        }

        private void AvatarImage_Click(object sender, RoutedEventArgs e)
        {

        }

        protected override bool ValidateInput()
        {
            bool dataOK = true;
            dataOK = UiHelper.Validator.ValidateFieldNonEmpty(Email, Email.Text, null, "Please enter an email address.")
                && UiHelper.Validator.ValidateEmail(Email, null) && dataOK;
            dataOK = UiHelper.Validator.ValidateFieldNonEmpty(Location, Location.Text, null, "Please enter the teams location.") && dataOK;
            dataOK = UiHelper.Validator.ValidateFieldNonEmpty(Firstname, Firstname.Text, null, "Please enter the team name.") && dataOK;
            dataOK = UiHelper.Validator.ValidateFieldNonEmpty(Description, Description.Text, null, "Please enter a description of the team.") && dataOK;
            return dataOK;
        }

        protected override void ResetInputs()
        {
            UiHelper.Disabler.DisableInputs(true, "Reloading");
            try
            {
                Logger.Debug("Reloading team " + _team.Username + " from the server");
                GwupeClientAppContext.CurrentAppContext.TeamManager.ReloadTeam(_team.Username);
                UiHelper.Validator.SetStatus("Reloaded team from the server.");
            }
            catch (Exception exception)
            {
                Logger.Error("Failed to reload the team : " + exception.Message, exception);
                UiHelper.Validator.SetError("Failed to reload this team.");
            }
            finally
            {
                UiHelper.Disabler.DisableInputs(false);
            }

        }

        protected override void CommitInput()
        {
            GwupeClientAppContext.CurrentAppContext.TeamManager.UpdateTeam(_team.Username);
        }

        protected override void CommitFailed(Exception exception)
        {
            var submissionError = exception as DataSubmissionException;
            if (submissionError != null)
            {
                if (submissionError.SubmitErrors.Count == 0)
                {
                    UiHelper.Validator.SetError("Error submitting");
                }
                else
                {
                    if (submissionError.SubmitErrors[0].ErrorCode == DataSubmitErrorCode.InUse &&
                        submissionError.SubmitErrors[0].FieldName.Equals("email"))
                    {
                        UiHelper.Validator.SetError("Email address in use", Email);
                    }
                    else if (submissionError.SubmitErrors[0].ErrorCode == DataSubmitErrorCode.EmailInvalid)
                    {
                        UiHelper.Validator.SetError("Email address is invalid", Email);
                    }
                    else
                    {
                        UiHelper.Validator.SetError("Unknown Error occurred");
                    }
                }
            }
            else
            {
                var elevationError = exception as ElevationException;
                if (elevationError != null)
                {
                    UiHelper.Validator.SetError("Password incorrect, cannot update team.");
                }
            }
        }

        protected override void CommitSuccessful()
        {
            UiHelper.Validator.SetStatus("Saved to server successfully.");
        }

        protected override void ResetStatus()
        {
            UiHelper.Validator.ResetStatus(new Control[] { Email, Firstname, Location, Description }, new Label[] { null, null, null, null });
        }

        private void AcceptClick(object sender, RoutedEventArgs e)
        {
            SendSubscribe(true);
        }

        private void SendSubscribe(bool subscribe)
        {
            ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    SubscribeRq request = new SubscribeRq { subscribe = subscribe, username = Team.Username };
                    GwupeClientAppContext.CurrentAppContext.ConnectionManager.Connection
                        .Request<SubscribeRq, SubscribeRs>(request);
                    GwupeClientAppContext.CurrentAppContext.TeamManager.ReloadTeam(Team.Username);
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to " + (subscribe ? "accept" : "decline") + " subscription to " + Team.Username, ex);
                }
            });
        }

        private void DeclineClick(object sender, RoutedEventArgs e)
        {
            SendSubscribe(false);
        }
    }
}
