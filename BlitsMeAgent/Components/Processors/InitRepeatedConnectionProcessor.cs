using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlitsMe.Cloud.Messaging.API;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using BlitsMe.Cloud.Repeater;
using log4net;

namespace BlitsMe.Agent.Components.Processors
{
    internal class InitRepeatedConnectionProcessor : UserToUserProcessor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (InitRepeatedConnectionProcessor));
        public InitRepeatedConnectionProcessor(BlitsMeClientAppContext appContext) : base(appContext)
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
