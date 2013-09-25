using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
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

        public PeerInfo SetupTunnel(String uniqueId, IPEndPoint facilitatorEndPoint, String encryptionKey)
        {
            IUDPTunnel tunnel = new UDPTunnel(0, uniqueId);
            tunnel.EncryptData = (ref byte[] data) => EncryptData(ref data, encryptionKey);
            tunnel.DecryptData = (ref byte[] data) => DecryptData(ref data, encryptionKey);
            PeerInfo self = tunnel.Wave(facilitatorEndPoint, 20000);
            Logger.Debug("After wave, local endpoints are " + self);
            if(self.ExternalEndPoint == null)
            {
                Logger.Warn("Failed to get external endpoint : " + self);
            }
            if (self.EndPoints.Count == 0)
            {
                throw new Exception("Failed to determine any local endpoints : " + self.ToString());
            }
            _pendingTunnels.Add(uniqueId, tunnel);
            return self;
        }

        // For now, just xor encryption
        private void DecryptData(ref byte[] data, String encryptionKey)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] ^= (byte)(encryptionKey[i%(encryptionKey.Length)]);
            }
        }

        private void EncryptData(ref byte[] data, String encryptionKey)
        {
            DecryptData(ref data, encryptionKey);
        }

        public IUDPTunnel CompleteTunnel(string uniqueId)
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
    }
}
