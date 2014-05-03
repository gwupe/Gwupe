using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using BlitsMe.Cloud.Messaging.API;
using BlitsMe.Cloud.Messaging.Elements;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using BlitsMe.Communication.P2P.P2P.Tunnel;
using BlitsMe.Communication.P2P.RUDP.Utils;
using log4net;

namespace BlitsMe.Agent.Components.Processors
{
    class InitUDPConnectionProcessor : Processor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(InitUDPConnectionProcessor));
        private readonly BlitsMeClientAppContext _appContext;

        internal InitUDPConnectionProcessor(BlitsMeClientAppContext appContext)
        {
            _appContext = appContext;
        }

        public Response process(Request req)
        {
            InitUDPConnectionRq request = (InitUDPConnectionRq) req;
            InitUDPConnectionRs response = new InitUDPConnectionRs();
            try
            {
                Engagement engagement = _appContext.EngagementManager.GetEngagement(request.username);
                if (engagement != null)// && !String.IsNullOrEmpty(engagement.SecondParty.ActiveShortCode) && (engagement.SecondParty.ActiveShortCode.Equals(request.shortCode)))
                {
                    // Hit the stun server
                    PeerInfo self = _appContext.P2PManager.SetupTunnel(request.uniqueId,
                        new IPEndPoint(IPAddress.Parse(request.facilitatorIP), Convert.ToInt32(request.facilitatorPort)),
                        request.encryptionKey);
                    response.externalEndPoint = self.ExternalEndPoint != null
                        ? new IpEndPointElement(self.ExternalEndPoint)
                        : null;
                    foreach (var internalEndPoint in self.InternalEndPoints)
                    {
                        response.internalEndPoints.Add(new IpEndPointElement(internalEndPoint));
                    }
                }
                else
                {
                    Logger.Warn("Incoming init UDP request from " + request.username + ", shortCode = " + request.shortCode + " is invalid, no such engagement.");
                }
            }
            catch (Exception e)
            {
                Logger.Warn("Failed to contact facilitator : " + e.Message,e);
                response.error = "FACILITATOR_ERROR";
                response.errorMessage = "Failed to contact facilitator";
            }

            return response;
        }
    }
}
