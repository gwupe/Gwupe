using System;

namespace BlitsMe.Agent.Components
{
    public class LoginDetails
    {
        public LoginDetails(String username, String passwordHash)
        {
            if (!String.IsNullOrEmpty(username) && !String.IsNullOrEmpty(passwordHash))
            {
                this.username = username;
                this.passwordHash = passwordHash;
            }
        }
        public String username { get; set; }
        public String passwordHash { get; set; }
        public String shortCode { get; set; }
        public String profile { get; set; }
        public String workstation { get; set; }
    }
}
