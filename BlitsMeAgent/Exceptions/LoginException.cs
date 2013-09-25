using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlitsMe.Cloud.Exceptions
{
    public class LoginException : Exception
    {
        public bool authFailure;
        public string failure;

        public const String INCORRECT_PASSWORD = "INCORRECT_PASSWORD";
        public LoginException(String message)
            : base(message)
        { 
        }
        public LoginException(String message, string failure)
            : base(message)
        {
            this.authFailure = failure.Equals(INCORRECT_PASSWORD) ? true : false;
            this.failure = failure;
        }
    }
}
