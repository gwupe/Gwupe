using System;

namespace Gwupe.Agent.Exceptions
{
    public class LoginException : Exception
    {
        public bool AuthFailure;
        public bool InvalidDetails;
        public string Failure;

        public const String INCORRECT_PASSWORD = "INCORRECT_PASSWORD";
        public const String EMPTY_DETAILS = "EMPTY_DETAILS";
        public LoginException(String message)
            : base(message)
        { 
        }

        public LoginException(String message, string failure)
            : base(message)
        {
            this.AuthFailure = failure.Equals(INCORRECT_PASSWORD);
            this.InvalidDetails =  failure.Equals(EMPTY_DETAILS);
            this.Failure = failure;
        }

    }
}
