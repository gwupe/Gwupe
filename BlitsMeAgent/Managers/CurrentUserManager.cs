using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using BlitsMe.Agent.Annotations;
using BlitsMe.Agent.Components;
using BlitsMe.Agent.Components.Person;
using BlitsMe.Agent.Components.Person.Presence;
using BlitsMe.Cloud.Messaging.Elements;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using log4net;

namespace BlitsMe.Agent.Managers
{
    internal class CurrentUserManager : INotifyPropertyChanged
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (CurrentUserManager));
        private readonly BlitsMeClientAppContext _appContext;
        private Person _currentUser;
        private Presence _currentUserPresence;
        private string _activeShortCode;

        public String ActiveShortCode
        {
            get { return _activeShortCode; }
            set { _activeShortCode = value; OnPropertyChanged("ActiveShortCode"); }
        }

        public event EventHandler CurrentUserChanged;

        public Person CurrentUser
        {
            get { return _currentUser; }
            private set { _currentUser = value; OnPropertyChanged("CurrentUser"); }
        }

        public Presence CurrentUserPresence
        {
            get { return _currentUserPresence; }
            private set { _currentUserPresence = value; OnPropertyChanged("CurrentUserPresence"); }
        }

        internal CurrentUserManager()
        {
            _appContext = BlitsMeClientAppContext.CurrentAppContext;
            _appContext.IdleChanged += OnIdleChanged;
            _appContext.LoginManager.LoggedOut += (sender, args) => Reset();
        }

        internal void SetUser(UserElement userElement, String shortCode)
        {
            CurrentUser = new Person(userElement);
            ActiveShortCode = shortCode;
            if(CurrentUserPresence == null)
            {
                CurrentUserPresence = new Presence();
            }
            CurrentUserPresence.SetIdleState(_appContext.IdleState);
            UpdatePresence(true);
            OnCurrentUserChanged(EventArgs.Empty);
        }

        private void OnIdleChanged(object sender, EventArgs eventArgs)
        {
            if (_appContext.ConnectionManager.IsOnline())
            {
                if (CurrentUserPresence != null)
                {
                    CurrentUserPresence.SetIdleState(_appContext.IdleState);
                    UpdatePresence();
                }
            }
        }

        public void UpdatePresence(bool ignoreLoginState = false)
        {
            if ((ignoreLoginState && _appContext.ConnectionManager.Connection.isEstablished()) || _appContext.ConnectionManager.IsOnline())
            {
                PresenceChangeRq request = new PresenceChangeRq()
                    {
                        presence = new PresenceElement()
                            {
                                mode = _currentUserPresence.Mode.ToString(),
                                type = _currentUserPresence.Type.ToString(),
                                priority = _currentUserPresence.Priority,
                                status = _currentUserPresence.Status
                            },
                        shortCode = ActiveShortCode,
                        user = _currentUser.Username
                    };
                try
                {
                    _appContext.ConnectionManager.Connection.RequestAsync<PresenceChangeRq,PresenceChangeRs>(request, HandlePresenceUpdateResponse);
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to update presence : " + e.Message,e);
                }
            }

        }

        private void HandlePresenceUpdateResponse(PresenceChangeRq presenceChangeRq, PresenceChangeRs presenceChangeRs, Exception ex)
        {
            if(ex == null)
            {
                Logger.Debug("Successfully updated presence : " + _currentUserPresence);
            } else
            {
                Logger.Error("Failed to update presence : " + ex.Message, ex);
            }
        }

        internal void ReloadCurrentUser()
        {
            CurrentUserRq request = new CurrentUserRq();
            CurrentUserRs response = _appContext.ConnectionManager.Connection.Request<CurrentUserRq, CurrentUserRs>(request);
            SetUser(response.userElement, response.shortCode);
        }

        internal void SaveCurrentUser(String elevateTokenId, String securityString, String password = null)
        {
            UpdateUserRq request = new UpdateUserRq
                {
                    userElement = new UserElement()
                        {
                            description = CurrentUser.Description,
                            firstname = CurrentUser.Firstname,
                            lastname = CurrentUser.Lastname,
                            email = CurrentUser.Email,
                            user = CurrentUser.Username,
                            location = CurrentUser.Location,
                            avatarData = CurrentUser.GetAvatarData(),
                            supporter = CurrentUser.Supporter,
                        },
                        tokenId = elevateTokenId,
                        securityKey = securityString,
                };
            if (password != null) request.password = password;
            UpdateUserRs response =
                _appContext.ConnectionManager.Connection.Request<UpdateUserRq, UpdateUserRs>(request);
            SetUser(response.userElement, ActiveShortCode);
        }

        public void OnCurrentUserChanged(EventArgs e)
        {
            EventHandler handler = CurrentUserChanged;
            if (handler != null) handler(this, e);
        }

        public void Reset()
        {
            Logger.Debug("Resetting Current User Manager, clearing current user and presence");
            _currentUser = null;
            _currentUserPresence = null;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
