using System;
using System.Net;
using System.Threading;
using BlitsMe.Cloud.Messaging.API;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using BlitsMe.Communication.P2P.P2P.Tunnel;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;
using BlitsMe.Communication.P2P.RUDP.Utils;
using log4net;

namespace BlitsMe.Agent.Components.Processors
{
    internal class ListenHandshakeProcessor : UserToUserProcessor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ListenHandshakeProcessor));
        private readonly BlitsMeClientAppContext _appContext;

        internal ListenHandshakeProcessor(BlitsMeClientAppContext appContext)
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