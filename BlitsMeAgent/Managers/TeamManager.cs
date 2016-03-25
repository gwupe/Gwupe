using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Resources;
using System.Windows.Controls;
using Gwupe.Agent.Components;
using Gwupe.Agent.Components.Person;
using Gwupe.Agent.Exceptions;
using Gwupe.Cloud.Exceptions;
using Gwupe.Cloud.Messaging.Elements;
using Gwupe.Cloud.Messaging.Request;
using Gwupe.Cloud.Messaging.Response;
using log4net;
using log4net.Repository.Hierarchy;

namespace Gwupe.Agent.Managers
{
    internal class TeamManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TeamManager));
        internal ObservableCollection<Team> Teams;
        private bool initialised = false;

        internal TeamManager()
        {
            Teams = new ObservableCollection<Team>();
            GwupeClientAppContext.CurrentAppContext.LoginManager.LoggedOut += (sender, args) => Reset();
        }

        public void RetrieveTeams()
        {
            var request = new TeamListRq();
            try
            {
                var response =
                    GwupeClientAppContext.CurrentAppContext.ConnectionManager.Connection.Request<TeamListRq, TeamListRs>
                        (request);
                Teams.Clear();
                if (response.teams != null)
                {
                    foreach (var newTeam in response.teams.Select(team => GwupeClientAppContext.CurrentAppContext.PartyManager.AddUpdatePartyFromElement(team) as Team))
                    {
                        // we need to take it off first (some items new, some updated)
                        newTeam.PropertyChanged -= TeamOnPropertyChanged;
                        newTeam.PropertyChanged += TeamOnPropertyChanged;
                        this.Teams.Add(newTeam);
                    }
                }
                initialised = true;
            }
            catch (Exception e)
            {
                Logger.Error("Failed to get the team", e);
                throw e;
            }
        }

        private void TeamOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName.Equals(nameof(Team.Admin)) ||
                propertyChangedEventArgs.PropertyName.Equals(nameof(Team.Player)))
            {
                // Admin or player has changed, we need to check this team
                CheckTeamAccess(sender as Team);
            }


        }

        public void Reset()
        {
            Teams.Clear();
            initialised = false;
        }

        public void ReloadTeam(string username)
        {
            try
            {
                var team = Teams.Single(chooseTeam => chooseTeam.Username.Equals(username));
                if (team != null)
                {
                    GwupeClientAppContext.CurrentAppContext.PartyManager.GetParty(username, true);
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to reload team " + username, e);
                throw;
            }
        }

        private void CheckTeamAccess(Team team)
        {
            // if the team is no longer applicable to me, then it must go
            if (team != null && !team.Admin && team.Player == PlayerMembership.none)
            {
                Logger.Debug("I am no longer a member of this team" + team.Username + ", removing team from my list.");
                Teams.Remove(team);
            }

        }

        public Team GetTeamByUniqueHandle(string uniqueHandle)
        {
            return Teams.Single(chooseTeam => chooseTeam.UniqueHandle.Equals(uniqueHandle));
        }

        public Team GetTeamByUsername(string username)
        {
            return Teams.Single(chooseTeam => chooseTeam.Username.Equals(username));
        }

        public void SignupTeam(String teamName, String uniqueHandle, String location, String email, bool supporter)
        {
            var signupTeamRq = new SignupTeamRq()
            {

                email = email,
                teamName = teamName,
                location = location,
                uniqueHandle = uniqueHandle,
                supporter = supporter
            };
            try
            {
                GwupeClientAppContext.CurrentAppContext.ConnectionManager.Connection.Request<SignupTeamRq, SignupTeamRs>
                    (signupTeamRq);
            }
            catch (MessageException<SignupTeamRs> ex)
            {
                Logger.Warn("Failed to signup team : " + ex.Message);
                var exception = CompileDataSubmissionException(ex.Response.signupErrors);
                throw exception;
            }
            catch (Exception ex)
            {
                Logger.Warn("Failed to signup : " + ex.Message);
                throw new DataSubmissionException();
            }
        }

        private static DataSubmissionException CompileDataSubmissionException(List<String> validationErrors)
        {
            var exception = new DataSubmissionException();
            if (validationErrors != null)
            {
                if (validationErrors.Contains(SignupRs.SignupErrorEmailAddressInUse))
                {
                    exception.SubmitErrors.Add(new DataSubmitError()
                    {
                        FieldName = "email",
                        ErrorCode = DataSubmitErrorCode.InUse
                    });
                }
                if (validationErrors.Contains(SignupRs.SignupErrorEmailAddressInvalid))
                {
                    exception.SubmitErrors.Add(new DataSubmitError()
                    {
                        FieldName = "email",
                        ErrorCode = DataSubmitErrorCode.EmailInvalid
                    });
                }
                if (validationErrors.Contains(SignupRs.SignupErrorUserExists))
                {
                    exception.SubmitErrors.Add(new DataSubmitError()
                    {
                        FieldName = "username",
                        ErrorCode = DataSubmitErrorCode.InUse
                    });
                }
                if (validationErrors.Contains(SignupRs.SignupErrorPasswordComplexity))
                {
                    exception.SubmitErrors.Add(new DataSubmitError()
                    {
                        FieldName = "password",
                        ErrorCode = DataSubmitErrorCode.NotComplexEnough
                    });
                }
            }
            return exception;
        }

        public void UpdateTeam(string username)
        {
            Team team = null;
            try
            {
                team = GetTeamByUsername(username);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to find team " + username);
                var exception = new DataSubmissionException();
                exception.SubmitErrors.Add(new DataSubmitError()
                {
                    FieldName = "username",
                    ErrorCode = DataSubmitErrorCode.InvalidKey
                });
                throw exception;
            }
            ElevateToken token = GwupeClientAppContext.CurrentAppContext.Elevate("In order to update this team, please enter your current password.");
            var request = new UpdateTeamRq()
            {
                teamElement = new TeamElement()
                {
                    avatarData = team.Avatar == null ? null : Convert.ToBase64String(team.Avatar),
                    description = team.Description,
                    email = team.Email,
                    firstname = team.Firstname,
                    location = team.Location,
                    supporter = team.Supporter,
                    user = team.Username
                },
                playerRequest = team.PlayerRequest,
                admin = team.Admin,
                securityKey = token.SecurityKey,
                tokenId = token.TokenId
            };
            try
            {
                var response = GwupeClientAppContext.CurrentAppContext.ConnectionManager.Connection
                    .Request<UpdateTeamRq, UpdateTeamRs>(
                        request);
                GwupeClientAppContext.CurrentAppContext.PartyManager.AddUpdatePartyFromElement(response.teamElement);
            }
            catch (MessageException<UpdateTeamRs> ex)
            {
                Logger.Error("Failed to update team", ex);
                var exception = CompileDataSubmissionException(ex.Response.validationErrors);
                throw exception;
            }
        }
    }
}
