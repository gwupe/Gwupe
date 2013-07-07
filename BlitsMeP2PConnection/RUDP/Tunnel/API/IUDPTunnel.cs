using System;
using System.ComponentModel;
using System.Net;
using BlitsMe.Communication.P2P.RUDP.Utils;

namespace BlitsMe.Communication.P2P.RUDP.Tunnel.API
{
    public delegate void ProcessPacket(byte[] data, String id);
    public delegate void EncryptData(ref byte[] data);
    public delegate void DecryptData(ref byte[] data);
    public delegate void ConnectionChangedEvent(object sender, EventArgs args);

    /* This interface is used for building classes which have the ability to establish and maintain
     * a UDP tunnel through firewalls using a middle man (stun type) service and synchronisation to
     * establish the tunnel.  Once established, it should also be responsible for maintaining the 
     * tunnel (via keep alives of some sort)
     */

    public interface IUDPTunnel
    {
        event ConnectionChangedEvent Connected;
        event ConnectionChangedEvent Disconnected;
        // a unique id for the tunnel
        String Id { get; set; }
        // Is the tunnel established
        bool IsTunnelEstablished { get; }
        // is it closing
        bool Closing { get; }
        // is it degraded (not sure if its still up)
        bool Degraded { get; }
        // Whats the latency on this link
        int PeerLatency { get; }
        // The remote IP of the tunnel
        IPAddress RemoteIp { get; }
        // Is it a local connection
        bool LocalConnection { get; }
        // Initiates syncing with a peer
        void SyncWithPeer(PeerInfo peerIp, int timeout);
        // Other side of the sync, waiting to be contacted by a syncWithPeer
        void WaitForSyncFromPeer(PeerInfo peerIp, int timeout);
        // Wave to a facilitator to get external information
        PeerInfo Wave(IPEndPoint facilitatorIP, int timeout);
        // Send a packet
        void SendData(byte[] data);
        // Receive a packet
        ProcessPacket ProcessData { get; set; }
        // Encryption
        EncryptData EncryptData { get; set; }
        DecryptData DecryptData { get; set; }
        // close the tunnel
        void Close();
    }
}