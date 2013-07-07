using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using BlitsMe.Cloud.Messaging.API;
using BlitsMe.Cloud.Messaging.Elements;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
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
                // Hit the stun server
                PeerInfo self = _appContext.P2PManager.SetupTunnel(request.uniqueId, new IPEndPoint(IPAddress.Parse(request.facilitatorIP), Convert.ToInt32(request.facilitatorPort)), request.encryptionKey);
                response.externalEndPoint = new IpEndPointElement(self.ExternalEndPoint);
                foreach (var internalEndPoint in self.InternalEndPoints)
                {
                    response.internalEndPoints.Add(new IpEndPointElement(internalEndPoint));
                }
            }
            catch (Exception e)
            {
                Logger.Info("Failed to contact facilitator : " + e.Message);
                response.error = "FACILITATOR_ERROR";
                response.errorMessage = "Failed to contact facilitator";
            }

            return response;
        }
    }
}
