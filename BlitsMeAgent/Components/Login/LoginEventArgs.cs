using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gwupe.Agent.Managers
{
    internal enum LoginState
    {
        LoggedIn,
        LoggedOut,
        LoggingIn,
        LoggingOut,
    };

    internal class LoginEventArgs
    {
        internal LoginState LoginState;
    }
}
