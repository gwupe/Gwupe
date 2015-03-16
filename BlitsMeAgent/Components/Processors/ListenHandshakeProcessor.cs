using System;
using System.Net;
using System.Threading;
using Gwupe.Cloud.Messaging.API;
using Gwupe.Cloud.Messaging.Request;
using Gwupe.Cloud.Messaging.Response;
using Gwupe.Communication.P2P.P2P.Tunnel;
using Gwupe.Communication.P2P.RUDP.Tunnel.API;
using Gwupe.Communication.P2P.RUDP.Utils;
using log4net;

namespace Gwupe.Agent.Components.Processors
{
    internal class ListenHandshakeProcessor : UserToUserProcessor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ListenHandshakeProcessor));
        private readonly GwupeClientAppContext _appContext;

        internal ListenHandshakeProcessor(GwupeClientAppContext appContext)
            : base(appContext)
        {
            _appContext = appContext;
        }

        internal override UserToUserResponse ProcessWithEngagement(Engagement engagement, UserToUserRequest req)
        {
            ListenHandshakeRq request = (ListenHandshakeRq)req;
            ListenHandshakeRs response = new ListenHandshakeRs();
            try
            {
                var peerInfo = new PeerInfo()
                    {
                        ExternalEndPoint =
                            request.externalEndPoint != null ? new IPEndPoint(IPAddress.Parse(request.externalEndPoint.address),
                                           request.externalEndPoint.port) : null,
                    };
                foreach (var ipEndPointElement in request.internalEndPoints)
                {
                    peerInfo.InternalEndPoints.Add(new IPEndPoint(IPAddress.Parse(ipEndPointElement.address), ipEndPointElement.port));
                }
                _appContext.P2PManager.ReceiveP2PTunnel(request.uniqueId, peerInfo);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to start listening for UDP traffic from peer : " + e.Message,e);
                response.error = "LISTEN_ERROR";
                response.errorMessage = "Failed to start listening for UDP traffic";
            }
            return response;
        }
    }
}