using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlitsMe.Agent.Components.Person;
using BlitsMe.Cloud.Messaging.Elements;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;

namespace BlitsMe.Agent.Managers
{
    internal class CurrentUserManager
    {
        private readonly BlitsMeClientAppContext _appContext;
        private Person _currentUser;
        public event EventHandler CurrentUserChanged;

        public Person CurrentUser
        {
            get { return _currentUser; }
        }

        internal CurrentUserManager(BlitsMeClientAppContext appContext)
        {
            _appContext = appContext;
        }

        internal void SetUser(UserElement userElement)
        {
            _currentUser = new Person(userElement);
            OnCurrentUserChanged(EventArgs.Empty);
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

    }
}
