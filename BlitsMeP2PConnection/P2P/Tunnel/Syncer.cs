using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using BlitsMe.Communication.P2P.RUDP.Packet;
using BlitsMe.Communication.P2P.RUDP.Packet.Tunnel;
using log4net;

namespace BlitsMe.Communication.P2P.P2P.Tunnel
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
        private Thread _syncListenerThread;
        private UdpClient _udpClient;

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
            _udpClient = udpClient;
            InitReceiverThread();
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

        private void InitReceiverThread()
        {
            _syncListenerThread = new Thread(ListenForPackets) { IsBackground = true, Name = "_syncListenerThread" };
            _syncListenerThread.Start();
        }

        private void ListenForPackets()
        {
            while (true)
            {
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                Byte[] bytes = null;
                try
                {
                    bytes = _udpClient.Receive(ref RemoteIpEndPoint);
                    BasicTunnelPacket packet = StandardUdpPacketFactory.instance.getPacket(bytes, RemoteIpEndPoint);
                    if (packet.type == BasicTunnelPacket.PKT_TYPE_SYNC_RS)
                    {
                        ProcessSyncRs((StandardSyncRsTunnelPacket)packet);
#if(DEBUG)
                        Logger.Debug("Got my sync response shutting down sync listener");
#endif
                        break;
                    }
                    else if (packet.type == BasicTunnelPacket.PKT_TYPE_SYNC_RQ)
                    {
                        ProcessSyncRq((StandardSyncRqTunnelPacket)packet);
#if(DEBUG)
                        Logger.Debug("Got my sync request shutting down sync listener");
#endif
                        break;
                    }
                    else if (packet.type == BasicTunnelPacket.PKT_TYPE_NOP)
                    {
#if(DEBUG)
                        Logger.Debug("Got a NOP");
#endif
                    }
                    else
                    {
                        Logger.Error("Waiting for a sync response, but got unknown packet");
                        break;
                    }
                }
                catch (SocketException e)
                {
#if(DEBUG)
                    Logger.Debug("Caught a socket exception [" + e.ErrorCode + "] : " + e.Message);
#endif
                    if (e.ErrorCode == 10004) // Interrupted
                    {
#if(DEBUG)
                        Logger.Debug("Socket has been interrupted, shutting down");
#endif
                        _udpClient.Close();
                        break;
                    }
                    else if (e.ErrorCode == 10054)
                    // Got ICMP connection closed ( we need to ignore this, hole punching causes these during init )
                    {
#if(DEBUG)
                        Logger.Debug("Remote host stated ICMP port closed, ignoring");
#endif
                    }
                    else
                    {
                        Logger.Warn("Caught a socket exception [" + e.ErrorCode +
                                    "], this looks spurious, ignoring : " + e.Message);
                        Logger.Error("Caught a socket exception [" + e.ErrorCode + "], shutting down read thread : " +
                                     e.Message);
                        _udpClient.Close();
                        break;
                    }
                }
                catch (ThreadAbortException e)
                {
#if DEBUG
                    Logger.Debug("Thread is aborting, closing : " + e.Message);
#endif
                    _udpClient.Close();
                    break;
                }
                catch (Exception e)
                {
                    Logger.Error("Exception while reading from UDP socket, shutting down read thread : " + e.Message, e);
                    // Most likely the link has failed (this side) or the app is closing
                    // either way, close the thread for the moment
                    _udpClient.Close();
                    break;
                }
            }
        }

        public void ProcessSyncRq(StandardSyncRqTunnelPacket packet)
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
                        _udpClient.Send(syncBytes, syncBytes.Length, packet.ip);
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
            _udpClient = udpClient;
            InitReceiverThread();
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
