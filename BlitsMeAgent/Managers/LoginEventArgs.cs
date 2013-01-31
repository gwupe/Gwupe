using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlitsMe.Agent.Managers
{
    public class LoginEventArgs
    {
        public Boolean Login;
        public Boolean Logout { set { Login = !value; } get { return !Login; } }
    }
}
