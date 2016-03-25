using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Gwupe.Agent.Components;
using Gwupe.Agent.Components.Notification;
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
        private readonly TeamManagement _teamManagement;
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

        public TeamSettingsControl(ContentPresenter disabler, TeamManagement teamManagement)
        {
            _teamManagement = teamManagement;
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

        private void AcceptPlayerRequestClick(object sender, RoutedEventArgs e)
        {
            SendSubscribe(true);
        }

        private void SendSubscribe(bool subscribe)
        {
            UiHelper.Disabler.DisableInputs(true, "Updating");
            ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    SubscribeRq request = new SubscribeRq {subscribe = subscribe, username = Team.Username};
                    GwupeClientAppContext.CurrentAppContext.ConnectionManager.Connection
                        .Request<SubscribeRq, SubscribeRs>(request);

                    // Hack the answer (actual notify comes through a little later)
                    Dispatcher.Invoke(new Action(() =>
                    {
                        Team.Player = subscribe
                            ? PlayerMembership.player
                            : PlayerMembership.none;
                    }));
                    // look through notifications to make sure that none related to this are there
                    foreach (
                        var notification in GwupeClientAppContext.CurrentAppContext.NotificationManager.Notifications)
                    {
                        if (notification is JoinTeamNotification)
                        {
                            var teamNotification = notification as JoinTeamNotification;
                            if (!String.IsNullOrEmpty(teamNotification.TeamUsername) &&
                                teamNotification.TeamUsername.Equals(Team.Username))
                            {
                                // this is the one, delete and break
                                GwupeClientAppContext.CurrentAppContext.NotificationManager.DeleteNotification(
                                    notification);
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(
                        "Failed to " + (subscribe ? "accept" : "decline") + " subscription to " + Team.Username, ex);
                    UiHelper.Validator.SetError("Failed to send subscription status to server.");
                }
                finally
                {
                    UiHelper.Disabler.DisableInputs(false);
                }
            });
        }

        private void DeclinePlayerRequestClick(object sender, RoutedEventArgs e)
        {
            SendSubscribe(false);
        }

        private void RecusePlayerClick(object sender, RoutedEventArgs e)
        {
            SendSubscribe(false);
        }

        private void RecuseAdminClick(object sender, RoutedEventArgs e)
        {

        }
    }
}
