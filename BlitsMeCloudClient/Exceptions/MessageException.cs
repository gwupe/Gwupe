using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gwupe.Cloud.Messaging.API;

namespace Gwupe.Cloud.Exceptions
{
    public class MessageException<T> : Exception where T : Response
    {
        // there is a message coming back which is details an error of some sort with the handling of the message
        public String ErrorCode;
        public T Response;

        public MessageException(T response)
            : base("[" + response.error + "] " + response.errorMessage)
        {
            Response = response;
            ErrorCode = response.error;
        }
    }
}
