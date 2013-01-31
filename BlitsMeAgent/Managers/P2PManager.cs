using System;
using System.Collections.Generic;
using System.Net;
using BlitsMe.Communication.P2P.RUDP.Tunnel;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;
using BlitsMe.Communication.P2P.RUDP.Utils;
using log4net;

namespace BlitsMe.Agent.Managers
{
    public class P2PManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(P2PManager));
        private readonly Dictionary<string, IUDPTunnel> _pendingTunnels;

        public P2PManager()
        {
            _pendingTunnels = new Dictionary<string, IUDPTunnel>();
        }

        public PeerInfo SetupTunnel(String uniqueId, IPEndPoint facilitatorEndPoint)
        {
            IUDPTunnel tunnel = new UDPTunnel(0);
            PeerInfo self = tunnel.Wave(facilitatorEndPoint,15000);
#if DEBUG
            Logger.Debug("Successfully waved to facilitator, my details are " + self.ToString());
#endif
            _pendingTunnels.Add(uniqueId, tunnel);
            return self;
        }

        public IUDPTunnel CompleteTunnel(string uniqueId)
        {
            // Get the tunnel and remove it from the list of pending _pendingTunnels.
            var tunnel = _pendingTunnels[uniqueId];
            _pendingTunnels.Remove(uniqueId);
            return tunnel;
        }

    }
}
