using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace BlitsMe.Cloud.Messaging.Response
{
    [DataContract]
    public class SignupRs : API.Response
    {
        public const String SignupErrorPasswordComplexity = "PASSWORD_COMPLEXITY_ERROR";
        public const String SignupErrorUsernameTaken = "USERNAME_TAKEN_ERROR";
        public const String SignupErrorEmailAddressInUse = "EMAIL_IN_USE_ERROR";

        public override string type { get { return "Signup-RS"; } set { } }
    }
}
