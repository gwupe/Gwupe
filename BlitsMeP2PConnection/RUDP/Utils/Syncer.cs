using System;
using System.Collections.Generic;
using System.Linq;
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

        private PeerInfo _expectedPeer;

        public Syncer()
        {

        }

        public void SyncWithPeer(PeerInfo peer, int timeout, UdpClient udpClient)
        {
            long waitTime = timeout * 10000;
            long startTime = DateTime.Now.Ticks;
#if DEBUG
            Logger.Debug("Syncing with peer " + peer + " from " + udpClient.Client.LocalEndPoint);
#endif

            StandardSyncRqTunnelPacket syncRq = new StandardSyncRqTunnelPacket();
            _syncEvent.Reset();
            do
            {
                byte[] syncBytes = syncRq.getBytes();
                udpClient.Send(syncBytes, syncBytes.Length, peer.externalEndPoint);
                if (DateTime.Now.Ticks - startTime > waitTime)
                {
#if(DEBUG)
                    Logger.Debug("Sync Timeout : " + (DateTime.Now.Ticks - startTime));
#endif
                    throw new TimeoutException("Timeout occured while attempting sync with peer " + peer.externalEndPoint);
                }
#if(DEBUG)
                Logger.Debug("Waiting to sync with " + peer.externalEndPoint);
#endif
            } while (!_syncEvent.WaitOne(1000));

            Logger.Info("Synchronisation with " + peer + " established");
        }

        public void ProcessSyncRs(StandardSyncRsTunnelPacket packet)
        {
            _syncEvent.Set();
        }

        public void ProcessSyncRq(StandardSyncRqTunnelPacket packet, UdpClient udpClient)
        {
            if (_expectedPeer.externalEndPoint.Equals(packet.ip) || _expectedPeer.internalEndPoint.Equals(packet.ip))
            {
                _syncRqEvent.Set();
                StandardSyncRsTunnelPacket syncRs = new StandardSyncRsTunnelPacket();
                byte[] syncBytes = syncRs.getBytes();
                udpClient.Send(syncBytes, syncBytes.Length, packet.ip);
            }
            else
            {
                Logger.Error("Got a sync request from an unexpected peer [" + packet.ip + "], dropping");
            }
        }

        public void WaitForSyncFromPeer(PeerInfo peer, int timeout, UdpClient udpClient)
        {
            long waitTime = timeout * 10000;
            long startTime = DateTime.Now.Ticks;
#if DEBUG
            Logger.Debug("Waiting for sync from peer " + peer + " to " + udpClient.Client.LocalEndPoint);
#endif
            StandardTunnelNopPacket nop = new StandardTunnelNopPacket();
            _expectedPeer = peer;
            _syncEvent.Reset();
            do
            {
                byte[] nopBytes = nop.getBytes();
                udpClient.Send(nopBytes, nopBytes.Length, peer.externalEndPoint);
                if (DateTime.Now.Ticks - startTime > waitTime)
                {
#if(DEBUG)
                    Logger.Debug("Sync Timeout : " + (DateTime.Now.Ticks - startTime));
#endif
                    throw new TimeoutException("Timeout occured while waiting for sync from peer " + peer.externalEndPoint);
                }
#if(DEBUG)
                Logger.Debug("Waiting for sync from " + peer.externalEndPoint);
#endif
            } while (!_syncRqEvent.WaitOne(1000));

            Logger.Info("Synchronisation with " + peer + " established");
        }
    }
}
