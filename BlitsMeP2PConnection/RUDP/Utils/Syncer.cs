using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using BlitsMe.Communication.P2P.RUDP.Packet.Tunnel;
using log4net;

namespace BlitsMe.Communication.P2P.RUDP.Utils
{
    public class Syncer
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Syncer));
        private readonly AutoResetEvent _syncEvent = new AutoResetEvent(false);
        private readonly AutoResetEvent _syncRqEvent = new AutoResetEvent(false);
        public String Id { get; private set; }

        private PeerInfo _expectedPeer;
        private IPEndPoint _lastSyncPacketIP;
        private IPEndPoint _lastSyncRqPacketIp;

        public Syncer(String id )
        {
            Id = id;
        }

        public bool SyncWithPeer(PeerInfo peer, int timeout, UdpClient udpClient)
        {
            long waitTime = timeout * 10000;
            long startTime = DateTime.Now.Ticks;
#if DEBUG
            Logger.Debug("Syncing with peer " + peer + " from " + udpClient.Client.LocalEndPoint + " to establish tunnel " + Id);
#endif

            StandardSyncRqTunnelPacket syncRq = new StandardSyncRqTunnelPacket();
            _syncEvent.Reset();
            do
            {
                byte[] syncBytes = syncRq.getBytes();
                udpClient.Send(syncBytes, syncBytes.Length, peer.internalEndPoint);
                udpClient.Send(syncBytes, syncBytes.Length, peer.externalEndPoint);
                if (DateTime.Now.Ticks - startTime > waitTime)
                {
#if(DEBUG)
                    Logger.Debug("Tunnel " + Id + ", sync Timeout : " + (DateTime.Now.Ticks - startTime));
#endif
                    throw new TimeoutException("Tunnel " + Id + ", timeout occured while attempting sync with peer " + peer.externalEndPoint + "/" + peer.internalEndPoint);
                }
#if(DEBUG)
                Logger.Debug("Tunnel " + Id + ", waiting to sync with " + peer.externalEndPoint + "/" + peer.internalEndPoint);
#endif
            } while (!_syncEvent.WaitOne(1000));
            var activeIp = _lastSyncPacketIP;

            Logger.Debug("Tunnel " + Id + ", synchronisation with " + activeIp + " established");
            return activeIp.Equals(peer.internalEndPoint);
        }

        public void ProcessSyncRs(StandardSyncRsTunnelPacket packet)
        {
            _lastSyncPacketIP = packet.ip;
            _syncEvent.Set();
        }

        public void ProcessSyncRq(StandardSyncRqTunnelPacket packet, UdpClient udpClient)
        {
            if (_expectedPeer.externalEndPoint.Equals(packet.ip) || _expectedPeer.internalEndPoint.Equals(packet.ip))
            {
                _lastSyncRqPacketIp = packet.ip;
                _syncRqEvent.Set();
                StandardSyncRsTunnelPacket syncRs = new StandardSyncRsTunnelPacket();
                byte[] syncBytes = syncRs.getBytes();
                udpClient.Send(syncBytes, syncBytes.Length, packet.ip);
            }
            else
            {
                Logger.Error("Tunnel " + Id + ", got a sync request from an unexpected peer [" + packet.ip + "], dropping");
            }
        }

        public bool WaitForSyncFromPeer(PeerInfo peer, int timeout, UdpClient udpClient)
        {
            long waitTime = timeout * 10000;
            long startTime = DateTime.Now.Ticks;
#if DEBUG
            Logger.Debug("Tunnel " + Id + ", waiting for sync from peer " + peer + " to " + udpClient.Client.LocalEndPoint);
#endif
            StandardTunnelNopPacket nop = new StandardTunnelNopPacket();
            _expectedPeer = peer;
            _syncEvent.Reset();
            do
            {
                byte[] nopBytes = nop.getBytes();
                udpClient.Send(nopBytes, nopBytes.Length, peer.internalEndPoint);
                udpClient.Send(nopBytes, nopBytes.Length, peer.externalEndPoint);
                if (DateTime.Now.Ticks - startTime > waitTime)
                {
#if(DEBUG)
                    Logger.Debug("Tunnel " + Id + ", sync Timeout : " + (DateTime.Now.Ticks - startTime));
#endif
                    throw new TimeoutException("Tunnel " + Id + ", timeout occured while waiting for sync from peer " + peer.externalEndPoint + "/" + peer.internalEndPoint);
                }
#if(DEBUG)
                Logger.Debug("Tunnel " + Id + ", waiting for sync from " + peer.externalEndPoint + "/" + peer.internalEndPoint);
#endif
            } while (!_syncRqEvent.WaitOne(1000));
            var activeIp = _lastSyncRqPacketIp;

            Logger.Debug("Tunnel " + Id + ", synchronisation with " + activeIp + " established");
            return activeIp.Equals(peer.internalEndPoint);
        }
    }
}
