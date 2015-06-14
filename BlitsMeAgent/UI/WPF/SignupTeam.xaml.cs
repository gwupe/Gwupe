using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Controls;
using Gwupe.Agent.Annotations;
using Gwupe.Agent.Components;
using Gwupe.Agent.Components.Person;
using Gwupe.Agent.UI.WPF.API;
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
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SignupTeam));
	    private SignupTeamDataContext _dataContext;
        public string TeamHandle { get; private set; }

        public SignupTeam(ContentPresenter disabler)
	    {
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

	    protected override bool CommitInput()
	    {
	        var signupTeamRq = GetSignupTeamRq();
            try
            {
                GwupeClientAppContext.CurrentAppContext.ConnectionManager.Connection.Request<SignupTeamRq, SignupTeamRs>(signupTeamRq);
                TeamHandle = signupTeamRq.uniqueHandle;
                return true;
            }
            catch (MessageException<SignupTeamRs> ex)
            {
                var errors = new DataSubmitErrorArgs();
                Logger.Warn("Failed to signup team : " + ex.Message);
                if (ex.Response.signupErrors != null)
                {
                    if (ex.Response.signupErrors.Contains(SignupRs.SignupErrorEmailAddressInUse))
                    {
                        errors.SubmitErrors.Add(new DataSubmitError()
                        {
                            FieldName = "email",
                            ErrorCode = SignupRs.SignupErrorEmailAddressInUse
                        });
                    }
                    if (ex.Response.signupErrors.Contains(SignupRs.SignupErrorEmailAddressInvalid))
                    {
                        errors.SubmitErrors.Add(new DataSubmitError()
                        {
                            FieldName = "email",
                            ErrorCode = SignupRs.SignupErrorEmailAddressInvalid
                        });
                    }
                    if (ex.Response.signupErrors.Contains(SignupRs.SignupErrorUserExists))
                    {
                        errors.SubmitErrors.Add(new DataSubmitError()
                        {
                            FieldName = "username",
                            ErrorCode = SignupRs.SignupErrorUserExists
                        });
                    }
                    if (ex.Response.signupErrors.Contains(SignupRs.SignupErrorPasswordComplexity))
                    {
                        errors.SubmitErrors.Add(new DataSubmitError()
                        {
                            FieldName = "password",
                            ErrorCode = SignupRs.SignupErrorPasswordComplexity
                        });
                    }
                }
                SubmissionErrors = errors;
            }
            catch (Exception ex)
            {
                Logger.Warn("Failed to signup : " + ex.Message);
                SubmissionErrors = new DataSubmitErrorArgs();
            }
	        return false;
	    }

	    private SignupTeamRq GetSignupTeamRq()
	    {
	        SignupTeamRq request = null;
	        if (!Dispatcher.CheckAccess())
	        {
	            Dispatcher.Invoke(new Action(() => { request = GetSignupTeamRq(); }));
	        }
	        else
	        {
	            request = new SignupTeamRq()
	            {
	                email = Email.Text.Trim(),
	                teamname = Teamname.Text.Trim(),
	                location = Location.Text.Trim(),
	                uniqueHandle = Username.Text.Trim(),
	                supporter = Supporter.IsChecked == true
	            };
	        }
	        return request;
	    }

	    protected override void CommitFailed()
	    {
	        if (SubmissionErrors.SubmitErrors.Count == 0)
	        {
	            UiHelper.Validator.SetError("Error submitting");
	        }
	        else
	        {
	            if (SubmissionErrors.SubmitErrors[0].ErrorCode.Equals(SignupRs.SignupErrorEmailAddressInUse))
	            {
	                UiHelper.Validator.SetError("Email address in use",Email);
                }
                else if (SubmissionErrors.SubmitErrors[0].ErrorCode.Equals(SignupRs.SignupErrorEmailAddressInvalid))
                {
                    UiHelper.Validator.SetError("Email address is invalid", Email);
                }
                else if (SubmissionErrors.SubmitErrors[0].ErrorCode.Equals(SignupRs.SignupErrorUserExists))
                {
                    UiHelper.Validator.SetError("This team already exists", Username);
                }
            }

	    }

	    protected override void CommitSuccessful()
	    {
	        if (!Dispatcher.CheckAccess())
	        {
	            Dispatcher.Invoke(new Action(CommitSuccessful));
	        }
	        else
	        {
	            TeamHandle = Teamname.Text;
	        }
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