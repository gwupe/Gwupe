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
        private IPEndPoint _lastSyncPacketIp;
        private IPEndPoint _lastSyncRqPacketIp;
        private readonly object _syncRsLock = new object();
        private readonly object _syncRqLock = new object();

        public Syncer(String id )
        {
            Id = id;
        }

        public IPEndPoint SyncWithPeer(PeerInfo peer, int timeout, UdpClient udpClient)
        {
            long waitTime = timeout * 10000;
            long startTime = DateTime.Now.Ticks;
            Logger.Debug("Syncing with peer " + peer + " from " + udpClient.Client.LocalEndPoint + " to establish tunnel " + Id);

            StandardSyncRqTunnelPacket syncRq = new StandardSyncRqTunnelPacket();
            _syncEvent.Reset();
            do
            {
                byte[] syncBytes = syncRq.getBytes();
                // attempt to sync with all endpoints, external is done first
                foreach (var endPoint in peer.EndPoints)
                {
                    udpClient.Send(syncBytes, syncBytes.Length, endPoint);
                }
                if (DateTime.Now.Ticks - startTime > waitTime)
                {
                    Logger.Debug("Tunnel " + Id + ", sync Timeout : " + (DateTime.Now.Ticks - startTime));
                    throw new TimeoutException("Tunnel " + Id + ", timeout occured while attempting sync with peer " + peer);
                }
                Logger.Debug("Tunnel " + Id + ", sent requests, waiting for sync response from " + peer);
            } while (!_syncEvent.WaitOne(1000));
            var activeIp = _lastSyncPacketIp;

            Logger.Debug("Tunnel " + Id + ", synchronisation with " + activeIp + " established");
            return activeIp;
        }

        public void ProcessSyncRs(StandardSyncRsTunnelPacket packet)
        {
            lock (_syncRsLock)
            {
                Logger.Debug("Got a sync response for " + Id + " from " + packet.ip);
                if (_lastSyncPacketIp == null || _lastSyncPacketIp.Equals(packet.ip))
                {
                    _lastSyncPacketIp = packet.ip;
                    _syncEvent.Set();
                }
                else
                {
                    Logger.Debug("Received a sync response packet for " + Id + " from " + packet.ip +
                                 " after we have already received our first one from " + _lastSyncPacketIp +
                                 ", ignoring");
                }
            }
        }

        public void ProcessSyncRq(StandardSyncRqTunnelPacket packet, UdpClient udpClient)
        {
            lock (_syncRqLock)
            {
                Logger.Debug("Got a sync request for " + Id + " from " + packet.ip + ", last sync packet was from " + (_lastSyncRqPacketIp == null ? "nowhere" : _lastSyncRqPacketIp.ToString()));
                if (_lastSyncRqPacketIp == null || _lastSyncRqPacketIp.Equals(packet.ip))
                {
                    if (_expectedPeer.EndPoints.Contains(packet.ip))
                    {
                        _lastSyncRqPacketIp = packet.ip;
                        _syncRqEvent.Set();
                        StandardSyncRsTunnelPacket syncRs = new StandardSyncRsTunnelPacket();
                        byte[] syncBytes = syncRs.getBytes();
                        udpClient.Send(syncBytes, syncBytes.Length, packet.ip);
                    }
                    else
                    {
                        Logger.Error("Tunnel " + Id + ", got a sync request from an unexpected peer [" + packet.ip +
                                     ", not " + _expectedPeer + "], dropping");
                    }
                }
                else
                {
                    Logger.Debug("Received a sync request packet for " + Id + " from " + packet.ip +
                                 " after we have already received our first one from " + _lastSyncRqPacketIp +
                                 ", ignoring");
                }
            }
        }

        public IPEndPoint WaitForSyncFromPeer(PeerInfo peer, int timeout, UdpClient udpClient)
        {
            long waitTime = timeout * 10000;
            long startTime = DateTime.Now.Ticks;
            Logger.Debug("Waiting for sync from peer " + peer + " to " + udpClient.Client.LocalEndPoint + " to establish tunnel " + Id);
            StandardTunnelNopPacket nop = new StandardTunnelNopPacket();
            _expectedPeer = peer;
            _syncEvent.Reset();
            do
            {
                byte[] nopBytes = nop.getBytes();
                // attempt to open comms with all endpoints, external is done first
                foreach (var endPoint in peer.EndPoints)
                {
                    udpClient.Send(nopBytes, nopBytes.Length, endPoint);
                }
                if (DateTime.Now.Ticks - startTime > waitTime)
                {
                    Logger.Debug("Tunnel " + Id + ", sync Timeout : " + (DateTime.Now.Ticks - startTime));
                    throw new TimeoutException("Tunnel " + Id + ", timeout occured while waiting for sync from peer " + peer);
                }
                Logger.Debug("Tunnel " + Id + ", sent nops, now waiting for sync request from " + peer);
            } while (!_syncRqEvent.WaitOne(1000));
            var activeIp = _lastSyncRqPacketIp;

            Logger.Debug("Tunnel " + Id + ", synchronisation with " + activeIp + " established");
            return activeIp;
        }
    }
}
