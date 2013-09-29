using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlitsMe.Cloud.Exceptions;
using BlitsMe.Cloud.Messaging.Response;

namespace BlitsMe.Agent.Exceptions
{
    class SignUpException : Exception
    {
        public List<String> errors;

        public SignUpException(MessageException<SignupRs> signupMessageException)
        {
            errors = signupMessageException.Response.signupErrors;
        }

        public SignUpException()
        {
        }
    }
}
