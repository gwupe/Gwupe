using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using BlitsMe.Agent.Components.Person;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using BlitsMe.Communication.P2P.P2P.Socket;
using BlitsMe.Communication.P2P.P2P.Socket.API;
using BlitsMe.Communication.P2P.P2P.Tunnel;
using BlitsMe.Communication.P2P.RUDP.Packet;
using log4net;
using Udt;
using Socket = Udt.Socket;

namespace BlitsMe.Agent.Managers
{
    internal class P2PManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(P2PManager));
        private readonly Dictionary<string, ITunnelEndpoint> _pendingTunnels;
        private readonly Dictionary<string, Action<ISocket>> _awaitingConnections;

        public P2PManager()
        {
            _pendingTunnels = new Dictionary<string, ITunnelEndpoint>();
            _awaitingConnections = new Dictionary<string, Action<ISocket>>();
            BlitsMeClientAppContext.CurrentAppContext.LoginManager.LoggedOut += (sender, args) => Reset();
        }

        // Called from RequestManager to establish a tunnel to a second party, its job is to wave and then save the result
        public PeerInfo SetupTunnel(String uniqueId, IPEndPoint facilitatorEndPoint, String encryptionKey)
        {
            BmUdtEncryptedSocket tunnelEndpoint = new BmUdtEncryptedSocket(encryptionKey);
            var self = tunnelEndpoint.Wave(facilitatorEndPoint);
            Logger.Debug("After wave, local endpoints are " + self);
            if (self.ExternalEndPoint == null)
            {
                Logger.Warn("Failed to get external endpoint : " + self);
            }
            if (self.EndPoints.Count == 0)
            {
                throw new Exception("Failed to determine any local endpoints : " + self.ToString());
            }

            _pendingTunnels.Add(uniqueId, tunnelEndpoint);
            return self;
        }

        // Called by local application to initialise a p2p connection to a second party
        private ISocket InitP2PConnection(Attendance secondParty, String connectionId)
        {
            var initRq = new InitP2PConnectionRq { shortCode = secondParty.ActiveShortCode, connectionId = connectionId };
            ITunnelEndpoint pendingTunnel;
            try
            {
                var response = BlitsMeClientAppContext.CurrentAppContext.ConnectionManager.Connection.Request<InitP2PConnectionRq, InitP2PConnectionRs>(initRq);
                // this will cause the server to initialise a tunnel endpoint and prepare it for connection
#if DEBUG
                Logger.Debug("Got response from p2p connection request");
#endif
                try
                {
                    pendingTunnel = GetPendingTunnel(response.uniqueId);

                    // setup my peers endpoints
                    var peer = GetPeerInfoFromResponse(response);
                    pendingTunnel.Sync(peer, response.uniqueId);
                    Logger.Info("Successfully completed outgoing tunnel with " + secondParty.Person.Username + "-" + secondParty.ActiveShortCode + " [" + response.uniqueId + "]");
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to sync with peer : " + e.Message, e);
                    throw new Exception("Failed to sync with peer : " + e.Message, e);
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to setup P2P Connection : " + e.Message, e);
                throw new Exception("Failed to setup P2P Connection : " + e.Message, e);
            }
            return pendingTunnel;
        }

        private static PeerInfo GetPeerInfoFromResponse(InitP2PConnectionRs response)
        {
            var peer = new PeerInfo()
            {
                ExternalEndPoint =
                    response.externalEndPoint != null
                        ? new IPEndPoint(IPAddress.Parse(response.externalEndPoint.address),
                            Convert.ToInt32(response.externalEndPoint.port))
                        : null,
            };
            foreach (var ipEndPointElement in response.internalEndPoints)
            {
                peer.InternalEndPoints.Add(new IPEndPoint(IPAddress.Parse(ipEndPointElement.address), ipEndPointElement.port));
            }
            return peer;
        }

        private ITunnelEndpoint GetPendingTunnel(string uniqueId)
        {
            // Get the tunnel and remove it from the list of pending _pendingTunnels.
            var tunnel = _pendingTunnels[uniqueId];
            _pendingTunnels.Remove(uniqueId);
            return tunnel;
        }

        public void Reset()
        {
            Logger.Debug("Resetting P2P Manager, clearing pending tunnels");
            foreach (var pendingTunnel in _pendingTunnels)
            {
                pendingTunnel.Value.Close();
            }
            _pendingTunnels.Clear(); ;
        }

        internal ISocket GetP2PConnection(Attendance secondParty, String connectionId)
        {
            // At this stage, we only support direct connections
            return InitP2PConnection(secondParty, connectionId);
        }

        // This is called by a waiting Function (like file send listener or rdp server) so that 
        // this callback can be called when its p2p connection is established
        internal void AwaitConnection(string connectionId, Action<ISocket> receiveConnection)
        {
            _awaitingConnections.Add(connectionId, receiveConnection);
            Logger.Debug("Awaiting a connection on " + connectionId);
        }

        // This is where we wait to receive a connection, requested by the server
        internal void ReceiveP2PTunnel(string connectionId, PeerInfo peerInfo)
        {
            ITunnelEndpoint pendingTunnel = GetPendingTunnel(connectionId);
            var receivingMethod = _awaitingConnections[connectionId];
            _awaitingConnections.Remove(connectionId);
            // now to complete the tunnel

            Thread thread = new Thread(() => RunSyncer(connectionId, peerInfo, pendingTunnel, receivingMethod)) { Name="waitforsync-" + connectionId, IsBackground = true };
            thread.Start();
        }

        private static void RunSyncer(string connectionId, PeerInfo peer, ITunnelEndpoint pendingTunnel, Action<ISocket> receivingMethod)
        {
            try
            {
                var activeIp = pendingTunnel.WaitForSync(peer, connectionId);
                Logger.Info("Successfully completed incoming tunnel with " + activeIp.Address + ":" + activeIp.Port + " [" + connectionId + "]");
                // call the callback method
                Logger.Debug("Handing over the the receiving method for this connection");
                receivingMethod(pendingTunnel);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to sync with peer [" + peer + "] for connection " + connectionId, ex);
            }
        }
    }
}
