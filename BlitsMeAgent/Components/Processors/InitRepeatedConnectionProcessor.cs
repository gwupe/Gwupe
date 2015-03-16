using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gwupe.Cloud.Messaging.API;
using Gwupe.Cloud.Messaging.Request;
using Gwupe.Cloud.Messaging.Response;
using Gwupe.Cloud.Repeater;
using log4net;

namespace Gwupe.Agent.Components.Processors
{
    internal class InitRepeatedConnectionProcessor : UserToUserProcessor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (InitRepeatedConnectionProcessor));
        public InitRepeatedConnectionProcessor(GwupeClientAppContext appContext) : base(appContext)
        {
        }

        internal override UserToUserResponse ProcessWithEngagement(Engagement engagement, UserToUserRequest rq)
        {
            var request = (InitRepeatedConnectionRq) rq;
            Logger.Debug("Got a request to connect to a repeated connection from " + rq.username + ", with id " + request.repeatId);
            return new InitRepeatedConnectionRs();
        }
    }
}
