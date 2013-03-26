using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlitsMe.Cloud.Messaging.Request
{
    public class LogoutRq : API.Request
    {
        public override string type
        {
            get { return "Logout-RQ"; }
            set {}
        }
    }
}
