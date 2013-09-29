using System;

namespace BlitsMe.Agent.Exceptions
{
    public class LoginException : Exception
    {
        public bool AuthFailure;
        public string Failure;

        public const String INCORRECT_PASSWORD = "INCORRECT_PASSWORD";
        public LoginException(String message)
            : base(message)
        { 
        }
        public LoginException(String message, string failure)
            : base(message)
        {
            this.AuthFailure = failure.Equals(INCORRECT_PASSWORD) ? true : false;
            this.Failure = failure;
        }
    }
}
