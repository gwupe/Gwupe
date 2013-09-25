using System;

namespace BlitsMe.Agent.Components
{
    public class LoginDetails
    {
        public String Username { get; set; }
        public String PasswordHash { get; set; }
        public String Profile { get; set; }
        public String Workstation { get; set; }
        public bool Ready { get { return !String.IsNullOrEmpty(Username) && !String.IsNullOrEmpty(PasswordHash); } }
    }
}
