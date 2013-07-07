using System;
using BlitsMe.Common.Security;
using BlitsMe.Communication.P2P.RUDP.Packet;
using BlitsMe.Communication.P2P.RUDP.Packet.Tunnel;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;
using BlitsMe.Communication.P2P.RUDP.Utils;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using BlitsMe.Communication.P2P.Exceptions;
using log4net;

namespace BlitsMe.Communication.P2P.RUDP.Tunnel
{
    /* This endPointManager is an implementation of the IDUP interface (see interface for more info).  It waves to the blitsme server platform and then sets
     * up a platform where it adds typing to packets (data/close/sync/ping/wave etc) to provide a endPointManager which requires no maintenance but this endPointManager
     * still has no packet arrival detection is it is a psuedo UDP connection, implement a TCP connection over this endPointManager if you want packet ordering
     * and guarantees 
     */

    public class UDPTunnel : IUDPTunnel
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(UDPTunnel));

        // Internal variables
        private PeerInfo _self; // me
        private PeerInfo _peer; // the syncrhonised endpoint
        private readonly UdpClient _udpClient; // the internal client obj
        private Waver _waver; // object used for waving
        private Syncer _syncer; // object used for syncing
        private Pinger _pinger; // object used for keeping synced link alive
        private Timer _pingTimer; // timer to ping every 30 seconds or check for pings every 30 seconds
        private const int PingIntervalMilliseconds = 30000;
        private const int AllowedPingFailCount = 3;
        private int _pingFailCount = 0;
        private bool _pinging = false;
        private long _lastPing;
        public bool LocalConnection { get; private set; }
        private IPEndPoint _remoteEndpoint;

        #region Events

        public event ConnectionChangedEvent Connected;

        protected virtual void OnConnected()
        {
            ConnectionChangedEvent handler = Connected;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        public event ConnectionChangedEvent Disconnected;

        protected virtual void OnDisconnected()
        {
            ConnectionChangedEvent handler = Disconnected;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        #endregion

        private bool _isTunnelEstablished;
        public bool Closing { private set; get; }
        public String Id { get; set; }

        // If there is a ping failure, its degraded
        public bool Degraded
        {
            get { return _pingFailCount > 0; }
        }

        // are we synced
        public bool IsTunnelEstablished
        {
            get { return _isTunnelEstablished; }
            private set
            {
                if (_isTunnelEstablished != value)
                {
                    _isTunnelEstablished = value;
                    if (value) { OnConnected(); } else { OnDisconnected(); }
                }
            }
        }

        public int PeerLatency { get; private set; } // latency between us an peer

        // Method variables
        public API.ProcessPacket ProcessData { get; set; }
        public EncryptData EncryptData { get; set; }
        public DecryptData DecryptData { get; set; }

        public IPAddress RemoteIp
        {
            get
            {
                if (_remoteEndpoint != null)
                {
                    return _remoteEndpoint.Address;
                }
                else
                {
                    return null;
                }
            }
        }

        public UDPTunnel(int port, String id = null)
        {
            if (id == null)
                Id = Util.getSingleton().generateString(8);
            else
                Id = id;
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);
            _udpClient = new UdpClient(endPoint);
            PeerLatency = 50;
            InitReceiver();
        }

        #region Tunnel Construction

        public void SyncWithPeer(PeerInfo peerIp, int timeout)
        {
            _syncer = new Syncer(Id);
            _remoteEndpoint = _syncer.SyncWithPeer(peerIp, timeout, _udpClient);
            _peer = peerIp;
            IsTunnelEstablished = true;
            StartPinger();
        }

        private void StartPinger()
        {
            _pingFailCount = 0;
            _pinger = new Pinger(Id);
            _pingTimer = new Timer(this.PingPeer, this, PingIntervalMilliseconds, PingIntervalMilliseconds);
        }

        private void PingPeer(Object stateInfo)
        {
            if (!_pinging)
            {
                _pinging = true;
                if (IsTunnelEstablished)
                {
                    try
                    {
#if(DEBUG)
                        Logger.Debug("Pinging peer " + _remoteEndpoint);
#endif
                        var ping = _pinger.Ping(_remoteEndpoint, 10000, _udpClient);
                        // Average out the last 10 pings
                        PeerLatency = ((PeerLatency * 9) + ping) / 10;
                        if (_pingFailCount > 0)
                        {
                            Logger.Info("Pinged successfully [" + ping + "ms (" + PeerLatency + "ms avg)], recovered from " + _pingFailCount + " ping failure(s).");
                        }
                        _pingFailCount = 0;
                    }
                    catch (Exception e)
                    {
                        _pingFailCount++;
                        Logger.Warn("Ping failed [failCount=" + _pingFailCount + ", max=" + AllowedPingFailCount + "] : " + e.Message);
                        if (_pingFailCount >= AllowedPingFailCount)
                        {
                            Logger.Error("Peer failed to respond after " + _pingFailCount + " attempts, shutting down link");
                            this.Close();
                        }
                    }
                }
                else
                {
                    _pingTimer.Dispose();
                }
                _pinging = false;
            }
        }

        public void WaitForSyncFromPeer(PeerInfo peerIp, int timeout)
        {
            _syncer = new Syncer(Id);
            _remoteEndpoint = _syncer.WaitForSyncFromPeer(peerIp, timeout, _udpClient);
            _peer = peerIp;
            IsTunnelEstablished = true;
            StartCheckerForPings();
        }

        private void StartCheckerForPings()
        {
            _lastPing = DateTime.Now.Ticks;
            _pingFailCount = 0;
            _pinger = new Pinger(Id);
            _pingTimer = new Timer(this.CheckForRecentPing, this, PingIntervalMilliseconds, PingIntervalMilliseconds);
        }

        private void CheckForRecentPing(object state)
        {
            long elapsedMillisecondsSinceLastPing = (long)new TimeSpan(DateTime.Now.Ticks - _lastPing).TotalMilliseconds;
            // We need to see if we have received a ping in the last interval millis (plus 10 seconds grace)
            if (elapsedMillisecondsSinceLastPing > (PingIntervalMilliseconds + 10000))
            {
                _pingFailCount++;
                Logger.Warn("Missing a ping from " + _remoteEndpoint + " [failCount=" + _pingFailCount + ", max=" + AllowedPingFailCount + "]");
            }
            else
            {
                if (_pingFailCount > 0)
                {
                    Logger.Info("Ping received, recovered from " + _pingFailCount + " missing ping(s).");
                }
                _pingFailCount = 0;
            }
            if (_pingFailCount >= AllowedPingFailCount)
            {
                Logger.Error("Missing too many pings from " + _remoteEndpoint + " [" + _pingFailCount + "], shutting down endPointManager");
                Close();
            }
        }

        public PeerInfo Wave(IPEndPoint facilitator, int timeout)
        {
            _waver = new Waver();
            _self = _waver.Wave(facilitator, timeout, _udpClient);
            return _self;
        }

        #endregion Tunnel Construction

        #region Sending

        public void SendData(byte[] data)
        {
            StandardTunnelDataPacket packet = new StandardTunnelDataPacket();
            if (EncryptData != null)
            {
                try
                {
                    EncryptData(ref data);
                }
                catch (Exception e)
                {
                    Logger.Error("Encryption failed, shutting down tunnel : " + e.Message, e);
                    this.Close();
                }
            }
            packet.data = data;
            SendPacket(packet);
        }

        private void SendPacket(BasicTunnelPacket packet)
        {
            if (IsTunnelEstablished)
            {
#if(DEBUG)
                Logger.Debug("Sending packet " + packet.ToString());
#endif
                byte[] sendBytes = packet.getBytes();
                _udpClient.Send(sendBytes, sendBytes.Length, _remoteEndpoint);
            }
            else
            {
                throw new ConnectionException("Cannot send packet [" + packet.ToString() + "], not currently synced with a peer");
            }
        }

        private void SendUDPClose()
        {
            StandardCloseTunnelPacket packet = new StandardCloseTunnelPacket();
            try
            {
                SendPacket(packet);
            }
            catch (Exception)
            {
                Logger.Error("Failed to send UDP close to peer " + _peer);
            }
        }

        #endregion Sending

        #region Receiving

        private Thread _receiveThread;

        private void InitReceiver()
        {
            _receiveThread = new Thread(ListenForPackets) { IsBackground = true, Name = "_receiveThread[" + Id + "]" };
            _receiveThread.Start();
        }

        private void ListenForPackets()
        {
#if(DEBUG)
            Logger.Debug("Started UDP reading thread");
#endif
            while (true)
            {
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                Byte[] bytes = null;
                try
                {
                    bytes = _udpClient.Receive(ref RemoteIpEndPoint);
                    BasicTunnelPacket packet = StandardUdpPacketFactory.instance.getPacket(bytes, RemoteIpEndPoint);
                    HandlePacket(packet);
                }
                catch (UnknownPacketException e)
                {
                    Logger.Error("Recieved an unknown packet type, dropping : " + e.Message);
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
                        this.Close();
                        break;
                    }
                    else if (e.ErrorCode == 10054) // Got ICMP connection closed ( we need to ignore this, hole punching causes these during init )
                    {
#if(DEBUG)
                        Logger.Debug("Remote host stated ICMP port closed, ignoring");
#endif
                    }
                    else if (e.ErrorCode == 10052) // Got a ttl expired which can happen since we are spamming both internal and external IP's, ignore
                    {
#if(DEBUG)
                        Logger.Debug("Ttl on one of our packets has expired, ignoring.");
#endif
                    }
                    else
                    {
                        Logger.Warn("Caught a socket exception [" + e.ErrorCode + "], this looks spurious, ignoring : " + e.Message);
                        Logger.Error("Caught a socket exception [" + e.ErrorCode + "], shutting down read thread : " + e.Message);
                        this.Close();
                        break;
                    }
                }
                catch (ThreadAbortException e)
                {
#if DEBUG
                    Logger.Debug("Thread is aborting, closing : " + e.Message);
#endif
                    this.Close();
                    break;
                }
                catch (Exception e)
                {
                    Logger.Error("Exception while reading from UDP socket, shutting down read thread : " + e.Message,e);
                    // Most likely the link has failed (this side) or the app is closing
                    // either way, close the thread for the moment
                    this.Close();
                    break;
                }
            }
        }

        private void HandlePacket(BasicTunnelPacket packet)
        {
#if(DEBUG)
            Logger.Debug("Looking for handler for packet " + packet.ToString() + " from ip " + packet.ip);
#endif
            // data packets must come from the established IP
            if (packet.type == BasicTunnelPacket.PKT_TYPE_DATA && packet.ip.Equals(_remoteEndpoint))
            {
#if(DEBUG)
                Logger.Debug("Got a data packet");
#endif
                if (ProcessData != null)
                {
                    try
                    {
                        byte[] data = packet.data;
                        if (DecryptData != null)
                        {
                            try
                            {
                                DecryptData(ref data);
                            }
                            catch (Exception e)
                            {
                                Logger.Error("Encryption failed, shutting down tunnel : " + e.Message, e);
                                this.Close();
                            }
                        }
                        // a new thread needs to handle this upstream otherwise upstream can hang the whole process
                        Thread processDataThread = new Thread(() => ProcessData(data, Id)) { IsBackground = true };
                        processDataThread.Name = "processTunnelData[" + Id + "-" + processDataThread.ManagedThreadId + "]";
                        processDataThread.Start();
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Failed to call processData on packet " + packet.ToString() + " : " + e.Message);
                    }
                }
                else
                {
                    Logger.Error("Cannot handle packet, no processData method");
                }
            }
            else if (packet.type == BasicTunnelPacket.PKT_TYPE_WAVE_RS)
            {
#if(DEBUG)
                Logger.Debug("Got a wave response from " + packet.ip + ", my address is " + ((StandardWaveTunnelRsPacket)packet).externalEndPoint + "/" + ((StandardWaveTunnelRsPacket)packet).internalEndPoint);
#endif
                if (_waver != null)
                {
                    _waver.ProcessWaveRs((StandardWaveTunnelRsPacket)packet);
                }
                else
                {
                    Logger.Error("Cannot process wave response, invalid wave handler");
                }
            }
            else if (packet.type == BasicTunnelPacket.PKT_TYPE_SYNC_RQ)
            {
#if(DEBUG)
                Logger.Debug("Got a sync request");
#endif
                if (_syncer != null)
                {
                    _syncer.ProcessSyncRq((StandardSyncRqTunnelPacket)packet, _udpClient);
                }
                else
                {
                    Logger.Error("Cannot process sync request, invalid sync handler");
                }
            }
            else if (packet.type == BasicTunnelPacket.PKT_TYPE_SYNC_RS)
            {
#if(DEBUG)
                Logger.Debug("Got a sync response");
#endif
                if (_syncer != null)
                {
                    _syncer.ProcessSyncRs((StandardSyncRsTunnelPacket)packet);
                }
                else
                {
                    Logger.Error("Cannot process sync response, invalid sync object");
                }
            }
            else if (packet.type == BasicTunnelPacket.PKT_TYPE_PONG && packet.ip.Equals(_remoteEndpoint))
            {
#if(DEBUG)
                Logger.Debug("Got a pong");
#endif
                if (_pinger != null)
                {
                    _pinger.ProcessPong((StandardPongTunnelPacket)packet);
                }
                else
                {
                    Logger.Error("Cannot process pong, invalid ping object");
                }
            }
            else if (packet.type == BasicTunnelPacket.PKT_TYPE_PING && packet.ip.Equals(_remoteEndpoint))
            {
#if(DEBUG)
                Logger.Debug("Got a ping");
#endif
                if (_pinger != null)
                {
                    _pinger.ProcessPing((StandardPingTunnelPacket)packet, _udpClient);
                    _lastPing = DateTime.Now.Ticks;
                }
                else
                {
                    Logger.Error("Cannot process ping, invalid ping object");
                }
            }
            else if (packet.type == BasicTunnelPacket.PKT_TYPE_NOP)
            {
#if(DEBUG)
                Logger.Debug("Got a nop");
#endif
            }
            else if (packet.type == BasicTunnelPacket.PKT_TYPE_CLOSE && packet.ip.Equals(_remoteEndpoint))
            {
#if(DEBUG)
                Logger.Debug("Got a close package, initialising close");
#endif
                this.Close();
            }
            else
            {
#if(DEBUG)
                Logger.Debug("Dropping unknown/invalid data packet " + packet.ToString());
#endif
            }
        }

        #endregion

        public void Close()
        {
            if (IsTunnelEstablished && !Closing)
            {
                // Set the state that we are closing
                this.Closing = true;
                Logger.Debug("Closing UDP tunnel to " + _peer);
                // If we initiated it, send a close to peer (fire and forget)
                SendUDPClose();
                // Mark the endPointManager as stopped
                this.IsTunnelEstablished = false;
                // Close the pinger
                if (_pingTimer != null)
                {
                    _pingTimer.Dispose();
                }
                // Close the UDP Client
                if (_udpClient != null)
                {
                    _udpClient.Close();
                }
                // shutdown the receive thread
                if (_receiveThread != null && _receiveThread.IsAlive)
                {
                    _receiveThread.Abort();
                }
                // We have stopped closing now
                this.Closing = false;
            }
        }
    }
}
