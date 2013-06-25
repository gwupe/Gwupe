using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using BlitsMe.Agent.Components;
using BlitsMe.Agent.Components.Person;
using BlitsMe.Agent.Components.Person.Presence;
using BlitsMe.Cloud.Messaging.Elements;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using log4net;

namespace BlitsMe.Agent.Managers
{
    internal class CurrentUserManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (CurrentUserManager));
        private readonly BlitsMeClientAppContext _appContext;
        private Person _currentUser;
        private Presence _currentUserPresence;
        public event EventHandler CurrentUserChanged;

        public Person CurrentUser
        {
            get { return _currentUser; }
        }

        public Presence CurrentUserPresence
        {
            get { return _currentUserPresence; }
        }

        internal CurrentUserManager(BlitsMeClientAppContext appContext)
        {
            _appContext = appContext;
            _appContext.IdleChanged += OnIdleChanged;
        }

        internal void SetUser(UserElement userElement)
        {
            _currentUser = new Person(userElement);
            if(CurrentUserPresence == null)
            {
                _currentUserPresence = new Presence();
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
                        shortCode = _currentUser.ShortCode,
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
            SetUser(response.userElement);
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
                            avatarData = CurrentUser.GetAvatarData()
                        },
                        tokenId = elevateTokenId,
                        securityKey = securityString,
                };
            if (password != null) request.password = password;
            UpdateUserRs response =
                _appContext.ConnectionManager.Connection.Request<UpdateUserRq, UpdateUserRs>(request);
            SetUser(response.userElement);
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
    }
}
