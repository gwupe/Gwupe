using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Windows.Controls;
using Gwupe.Agent.Annotations;
using Gwupe.Agent.Components;
using Gwupe.Agent.Components.Person;
using Gwupe.Agent.Exceptions;
using Gwupe.Agent.UI.WPF.API;
using Gwupe.Agent.UI.WPF.Team;
using Gwupe.Agent.UI.WPF.Utils;
using Gwupe.Cloud.Exceptions;
using Gwupe.Cloud.Messaging.Request;
using Gwupe.Cloud.Messaging.Response;
using log4net;

namespace Gwupe.Agent.UI.WPF
{
    /// <summary>
    /// Interaction logic for SignupTeam.xaml
    /// </summary>
    public partial class SignupTeam : GwupeDataCaptureForm
    {
        private readonly TeamManagement _teamManagementWindow;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SignupTeam));
        private SignupTeamDataContext _dataContext;
        public string TeamHandle { get; private set; }

        public SignupTeam(ContentPresenter disabler, TeamManagement teamManagementWindow)
        {
            _teamManagementWindow = teamManagementWindow;
            _dataContext = new SignupTeamDataContext();
            this.DataContext = _dataContext;
            this.InitializeComponent();
            InitGwupeDataCaptureForm(disabler, null, ErrorText);
            ProcessingWord = "Creating";
        }

        protected override bool ValidateInput()
        {
            bool dataOK = true;
            dataOK = UiHelper.Validator.ValidateFieldNonEmpty(Email, Email.Text, null, "Please enter your email address")
                && UiHelper.Validator.ValidateEmail(Email, null) && dataOK;
            dataOK = UiHelper.Validator.ValidateFieldNonEmpty(Location, Location.Text, null, "Please enter your location", "City, Country") && dataOK;
            dataOK = UiHelper.Validator.ValidateFieldNonEmpty(Username, Username.Text, null, "Please enter your preferred Team handle") && dataOK;
            dataOK = UiHelper.Validator.ValidateFieldMatches(Username, Username.Text, null,
                                                    "Team handle can only use normal characters", "", ".*[^a-zA-Z0-9_\\-\\.].*") && dataOK;
            dataOK = UiHelper.Validator.ValidateFieldMatches(Username, Username.Text, null,
                                                    "Team handle cannot contain spaces", "", ".* .*") && dataOK;
            dataOK = UiHelper.Validator.ValidateFieldNonEmpty(Teamname, Teamname.Text, null, "Please enter your team's name", "Team Name") && dataOK;

            return dataOK;
        }

        protected override void ResetStatus()
        {
            UiHelper.Validator.ResetStatus(new Control[] { Email, Username, Location, Teamname }, new Label[] { null, null, null, null });
        }

        protected override void ResetInputs()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action(ResetInputs));
            }
            else
            {
                Email.Text = "";
                Username.Text = "";
                Location.Text = "";
                Teamname.Text = "";
                Supporter.IsChecked = false;
            }
        }

        protected override void CommitInput()
        {
            try
            {
                GwupeClientAppContext.CurrentAppContext.TeamManager.SignupTeam(
                    UiUtils.GetFieldValue<String>(Teamname, Dispatcher),
                    UiUtils.GetFieldValue<String>(Username, Dispatcher),
                    UiUtils.GetFieldValue<String>(Location, Dispatcher),
                    UiUtils.GetFieldValue<String>(Email, Dispatcher),
                    UiUtils.GetFieldValue<bool>(Supporter, Dispatcher)
                    );
                TeamHandle = UiUtils.GetFieldValue<String>(Username, Dispatcher);
            }
            catch (DataSubmissionException ex)
            {
                Logger.Error("Failed to signup team : " + ex.Message);
                throw ex;
            }
            catch (Exception ex)
            {
                Logger.Warn("Failed to signup : " + ex.Message);
                throw new DataSubmissionException();
            }
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
                    else if (submissionError.SubmitErrors[0].ErrorCode == DataSubmitErrorCode.InUse &&
                             submissionError.SubmitErrors[0].FieldName.Equals("username"))
                    {
                        UiHelper.Validator.SetError("This team already exists", Username);
                    }
                    else
                    {
                        UiHelper.Validator.SetError("Unknown Error occurred");
                    }

                }
            }
            else
            {
                UiHelper.Validator.SetError("Unknown Error occurred.");
            }

        }

        protected override void CommitSuccessful()
        {
            ResetInputs();
            UiHelper.Disabler.DisableInputs(true, "Refreshing");
            GwupeClientAppContext.CurrentAppContext.TeamManager.RetrieveTeams();
            UiHelper.Disabler.DisableInputs(false);
            _teamManagementWindow.SelectTeam(TeamHandle);
        }

    }

    public class SignupTeamDataContext : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}