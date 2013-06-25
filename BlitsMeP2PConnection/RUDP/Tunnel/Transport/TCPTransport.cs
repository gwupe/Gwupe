using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using BlitsMe.Communication.P2P.Exceptions;
using BlitsMe.Communication.P2P.RUDP.Packet;
using BlitsMe.Communication.P2P.RUDP.Packet.API;
using BlitsMe.Communication.P2P.RUDP.Packet.TCP;
using BlitsMe.Communication.P2P.RUDP.Socket.API;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;
using BlitsMe.Communication.P2P.RUDP.Utils;
using log4net;

namespace BlitsMe.Communication.P2P.RUDP.Tunnel.Transport
{
    public class TCPTransport : ITCPTransport
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TCPTransport));
        private readonly TCPConnectionHelper _tcpConnectionHelper;
        private readonly Dictionary<String, NamedTCPListener> _listeningNamedTCPEndPoints; // listeners
        private readonly TcpConnectionList _activeTCPConnections; // All the Active Connections
        private readonly ITransportManager _transportManager;

        private byte _lastConnectionId = byte.MaxValue;

        // Locks for various activities
        private readonly object _connectLock = new Object();
        private readonly object _disconnectLock = new Object();
        private readonly object _receiveConnectLock = new Object();
        private readonly object _lastConnectionLock = new Object();
        private const byte MaxSupportedProtocolId = 2;

        public ITransportManager TransportManager { get { return _transportManager; } }

        public TCPTransport(ITransportManager transportManager)
        {
            _transportManager = transportManager;
            _activeTCPConnections = new TcpConnectionList();
            _listeningNamedTCPEndPoints = new Dictionary<String, NamedTCPListener>();
            _tcpConnectionHelper = new TCPConnectionHelper(this);
        }

        #region Incoming Packet Handling

        public void ProcessPacket(byte[] packetData)
        {
            try
            {
                BasicTcpPacket packet = StandardTcpPacketFactory.instance.getPacket(packetData);
#if(DEBUG)
                Logger.Debug("Got packet : " + packet.ToString());
#endif
                switch (packet.Type)
                {
                    case BasicTcpPacket.PKT_TYPE_DISCONNECT:
                        ProcessDisconnectPacket((StandardDisconnectPacket)packet);
                        break;
                    case BasicTcpPacket.PKT_TYPE_ACK:
                        ProcessAckPacket((StandardAckPacket)packet);
                        break;
                    case BasicTcpPacket.PKT_TYPE_DATA:
                        ProcessDataPacket((StandardTcpDataPacket)packet);
                        break;
                    case BasicTcpPacket.PKT_TYPE_CONNECT_NAME_RQ:
                        ProcessNamedConnectionRequest((StandardNamedConnectRqPacket)packet);
                        break;
                    case BasicTcpPacket.PKT_TYPE_CONNECT_NAME_RS:
                        _tcpConnectionHelper.ProcessConnectRs((StandardNamedConnectRsPacket)packet);
                        break;
                    case BasicTcpPacket.PKT_TYPE_CONNECT_RS_ACK:
                        ProcessConnectRsAck((StandardConnectRsAckPacket)packet);
                        break;
                    case BasicTcpPacket.PKT_TYPE_DISCONNECT_RS:
                        _tcpConnectionHelper.ProcessDisconnectRs((StandardDisconnectRsPacket)packet);
                        break;
                    default:
                        throw new UnknownPacketException("Failed to handle packet " + packet);
                }
            }
            catch (UnknownPacketException e)
            {
                Logger.Error("Failed to process TCP packet, unknown type : " + e.Message);
            }
        }

        private void ProcessDisconnectPacket(StandardDisconnectPacket packet)
        {
            lock (_disconnectLock)
            {
                try
                {
                    TcpConnectionHolder connectionHolder = _activeTCPConnections.GetRemoteConnection(packet.ConnectionId);
                    // this closes everything, socket, connection and removes it from the list.
                    _activeTCPConnections.Remove(connectionHolder.Connection.ConnectionId);
                    connectionHolder.Connection.Close();
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to close connection [remote id = " + packet.ConnectionId + "] : " + e.Message);
                }
                SendData(new StandardDisconnectRsPacket(packet.ConnectionId));
            }
        }

        private void ProcessDataPacket(StandardTcpDataPacket packet)
        {
            try
            {
                TcpConnectionHolder conn = _activeTCPConnections.GetRemoteConnection(packet.ConnectionId);
                // If the connect response ack packet was lost, its possible that this connection is not open, we need to open it now
                if (!conn.Connection.Established)
                {
#if DEBUG
                    Logger.Debug("Outgoing Connection [" + conn.Connection.ConnectionId + "] received data, means our connect rs ack was lost, auto opening");
#endif
                    conn.Connection.Open();
                }
                // Lock receiving while we process this packet (we will still be able to send)
                // Now TcpConnection doesn't have to be threadsafe, but each connection 
                // can process data independently at full speed.
                lock (conn.ReceiveLock)
                {
                    // now process the dataPacket;
                    conn.Connection.ProcessDataPacket(packet);
                }
            }
            catch (ConnectionException e)
            {
                Logger.Error("Dropping data packet [" + packet + "], failed to get a connection : " + e.Message);
            }
        }

        private void ProcessAckPacket(StandardAckPacket packet)
        {
            try
            {
                TcpConnectionHolder conn = _activeTCPConnections.GetRemoteConnection(packet.ConnectionId);
                // Lock sending while we process this packet (we will still be able to receive)
                // Now TcpConnection doesn't have to be threadsafe, but each connection 
                // can process acks independently at full speed.
                lock (conn.SendLock)
                {
                    // now process the ackPacket;
                    conn.Connection.ProcessAck(packet);
                }
            }
            catch (ConnectionException e)
            {
                Logger.Error("Dropping ack packet [" + packet + "], failed to get a connection : " + e.Message);
            }
        }

        private void ProcessConnectRsAck(StandardConnectRsAckPacket packet)
        {
            TcpConnectionHolder conn = null;
            try
            {
                conn = _activeTCPConnections.GetRemoteConnection(packet.ConnectionId);
                try
                {
                    conn.Connection.Open();
                    conn.StopTimerToOpen();
                }
                catch (Exception e)
                {
                    Logger.Error("Error starting connection [" + conn.Connection.ConnectionId + "] : " + e.Message);
                    if (conn != null)
                    {
                        conn.Connection.Close();
                    }
                }
            }
            catch (ConnectionException e)
            {
                Logger.Error("Dropping connect rs ack packet [" + packet + "], failed to get a connection : " + e.Message);
            }
        }


        private void ProcessNamedConnectionRequest(StandardNamedConnectRqPacket packet)
        {
            StandardNamedConnectRsPacket response = new StandardNamedConnectRsPacket();
            // By default the conn id going back is the conn id of the request
            // this will change if the connection is a success, then it will be our conn id.
            response.ConnectionId = packet.ConnectionId;
            lock (_receiveConnectLock)
            {
                // If this connection exists
                if (_activeTCPConnections.IsRemoteConnection(packet.ConnectionId))
                {
#if(DEBUG)
                    Logger.Debug("Request for connection id [" + packet.ConnectionId + "] already established, resending response");
#endif
                    TcpConnectionHolder tcpConnectionHolder = _activeTCPConnections.GetRemoteConnection(packet.ConnectionId);
                    response.remoteConnectionId = tcpConnectionHolder.Connection.ConnectionId;
                    response.protocolId = tcpConnectionHolder.Connection.ProtocolId;
                    // we can respond that we are already established here
                    response.success = true;
                }
                else
                {
                    lock (_listeningNamedTCPEndPoints)
                    {
                        if (_listeningNamedTCPEndPoints.ContainsKey(packet.connectionName))
                        {
                            byte localConnectionId = GetNextConnectionId();
#if(DEBUG)
                            Logger.Debug("Found a listener for " + packet.connectionName + " connecting local conn [" + localConnectionId + "] to remote conn [" + packet.ConnectionId + "]");
#endif
                            byte agreedProtocolId = packet.protocolId;
                            if (packet.protocolId > MaxSupportedProtocolId)
                            {
#if(DEBUG)
                                Logger.Debug("They wanted protocol " + packet.protocolId + ", but I only support up to " + MaxSupportedProtocolId + ", so I propose it.");
#endif
                                agreedProtocolId = MaxSupportedProtocolId;
                            }
#if(DEBUG)
                            Logger.Debug("I have agreed to protocol " + agreedProtocolId);
#endif
                            response.protocolId = agreedProtocolId;
                            ITcpTransportLayer connection = null;
                            switch (agreedProtocolId)
                            {
                                case (1):
                                    {
                                        connection = new TcpTransportLayerOne4One(this, localConnectionId, packet.ConnectionId);
                                        break;
                                    }
                                case (2):
                                    {
                                        connection = new TcpTransportLayerSlidingWindow(this, localConnectionId, packet.ConnectionId);
                                        break;
                                    }
                                default:
                                    {
                                        Logger.Error("Failed to agree on a tcp protocol, I don't support " + agreedProtocolId);
                                        break;
                                    }
                            }
                            if (connection != null)
                            {
                                // only add the connection if 
                                response.success =
                                    _listeningNamedTCPEndPoints[packet.connectionName].connectCallback(connection.Socket);
                                if (response.success)
                                {
                                    _activeTCPConnections.Add(connection);
                                    response.remoteConnectionId = localConnectionId;
                                }
                            }
                            else
                            {
                                response.success = false;
                            }
                        }
                        else
                        {
                            Logger.Warn("No listener for " + packet.connectionName);
                            response.success = false;
                        }
                    }
                }
            }
#if(DEBUG)
            Logger.Debug("Sending connection response : " + response);
#endif
            _transportManager.SendData(response);
            if (response.success)
            {
                // This sends a positive response, we would like to get an Ack of this connection response
                // which will open this connection, but should that ack not arrive, let set a timer to automatically
                // open this connection after 10 seconds
                // we know that the sender will retry the connection should it not receive our response.
                _activeTCPConnections.GetLocalConnection(response.remoteConnectionId).StartTimerToOpen();
            }
        }

        #endregion

        public void SendData(ITcpPacket packet)
        {
            _transportManager.SendData(packet);
        }

        public ITcpOverUdptSocket OpenConnection(string endPoint)
        {
            return OpenConnection(endPoint, MaxSupportedProtocolId);
        }

        #region Connection Handling

        public ITcpOverUdptSocket OpenConnection(String endPoint, byte protocolId)
        {
            lock (_connectLock)
            {
                byte connectionId = GetNextConnectionId();
                ITcpTransportLayer connection = _tcpConnectionHelper.ConnectNamed(connectionId, endPoint, 10000, protocolId);
#if(DEBUG)
                Logger.Debug("Successfully connected to service " + endPoint + " local connection [" + connectionId + "], remote connection [" + connection.RemoteConnectionId + "]");
#endif
                _activeTCPConnections.Add(connection);
                _tcpConnectionHelper.AckConnectionResponse(connectionId);
                connection.Open();
                return connection.Socket;
            }
        }

        private byte GetNextConnectionId()
        {
            lock (_lastConnectionLock)
            {
                byte saveLast = _lastConnectionId;
                while (_activeTCPConnections.IsLocalConnection(++_lastConnectionId))
                {
                    if (_lastConnectionId != saveLast)
                    {
                        throw new ConnectionException("Cannot get next connection id, all connections in use");
                    }
                }
            }
#if(DEBUG)
            Logger.Debug("Found next connection id " + _lastConnectionId);
#endif
            return _lastConnectionId;
        }

        public void Listen(string endPoint, Func<ITcpOverUdptSocket, bool> callback)
        {
            lock (_listeningNamedTCPEndPoints)
            {
                if (_listeningNamedTCPEndPoints.ContainsKey(endPoint))
                {
                    throw new ConnectionException("Cannot listen on endpoint " + endPoint + ", there is already a listener for this");
                }
                _listeningNamedTCPEndPoints.Add(endPoint, new NamedTCPListener(endPoint, callback));
            }
#if(DEBUG)
            Logger.Debug("Listening for " + endPoint);
#endif
        }

        public void StopListen(string endPoint)
        {
            Logger.Debug("Stopping listening on " + endPoint);
            lock (_listeningNamedTCPEndPoints)
            {
                if (_listeningNamedTCPEndPoints.ContainsKey(endPoint))
                {
                    _listeningNamedTCPEndPoints.Remove(endPoint);
                }
                else
                {
                    Logger.Warn("Attempted to stop listening on " + endPoint + ", but it doesn't exist");
                }
            }
        }

        // Close a client connection with an id
        public void CloseConnection(byte connectionId)
        {
            if (_activeTCPConnections.IsLocalConnection(connectionId))
            {
                var connectionHolder = _activeTCPConnections.Remove(connectionId);
                connectionHolder.Connection.Close();
                _tcpConnectionHelper.CloseConnection(connectionId);
            }
        }

        #endregion

        public void Close(bool initiatedBySelf)
        {
            foreach (TcpConnectionHolder tcpConnectionHolder in _activeTCPConnections.GetList())
            {
                CloseConnection(tcpConnectionHolder.Connection.ConnectionId);
            }
            _listeningNamedTCPEndPoints.Clear();
        }
    }

    internal class NamedTCPListener
    {
        internal String listenerName;
        internal Func<ITcpOverUdptSocket, bool> connectCallback;

        internal NamedTCPListener(String name, Func<ITcpOverUdptSocket, bool> callback)
        {
            this.listenerName = name;
            this.connectCallback = callback;
        }

    }

    /* This class exists because we need metadata and locking structures about the connection that isn't stored in the connection, so we create
     * a local holder class for it, which contains the connection and its meta data and locks
     */
    internal class TcpConnectionHolder
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TcpConnectionHolder));
        internal ITcpTransportLayer Connection;
        internal Object SendLock;
        internal Object ReceiveLock;
        private Timer autoOpenTimer;

        internal TcpConnectionHolder(ITcpTransportLayer conn)
        {
            Connection = conn;
            SendLock = new Object();
            ReceiveLock = new Object();
            autoOpenTimer = new Timer(10000);
            autoOpenTimer.Elapsed += delegate
                {
                    if (!Connection.Established)
                    {
#if DEBUG
                        Logger.Debug("Connection timer elapsed, auto opening connection " + conn.ConnectionId);
#endif
                        Connection.Open();
                    }
                };
        }

        internal void StartTimerToOpen()
        {
            StopTimerToOpen();
            autoOpenTimer.Start();
        }

        public void StopTimerToOpen()
        {
            if (autoOpenTimer.Enabled)
            {
                autoOpenTimer.Stop();
            }
        }
    }

    /* This class is for keeping a list of current tcp connections 
     * as well as providing a lookup function
     */

    internal class TcpConnectionList
    {
        private readonly Dictionary<byte, TcpConnectionHolder> _list;
        private readonly Dictionary<byte, byte> _remoteLocalIdMap;

        internal TcpConnectionList()
        {
            _list = new Dictionary<byte, TcpConnectionHolder>();
            _remoteLocalIdMap = new Dictionary<byte, byte>();
        }

        internal void Add(ITcpTransportLayer connection)
        {
            lock (_list)
            {
                _list.Add(connection.ConnectionId, new TcpConnectionHolder(connection));
                _remoteLocalIdMap.Add(connection.RemoteConnectionId, connection.ConnectionId);
            }
        }

        internal TcpConnectionHolder Remove(byte localConnectionId)
        {
            TcpConnectionHolder holder = null;
            lock (_list)
            {
                if (_list.TryGetValue(localConnectionId, out holder))
                {
                    _list.Remove(localConnectionId);
                    if (_remoteLocalIdMap.ContainsKey(holder.Connection.RemoteConnectionId))
                    {
                        _remoteLocalIdMap.Remove(holder.Connection.RemoteConnectionId);
                    }
                }
            }
            return holder;
        }

        internal TcpConnectionHolder[] GetList()
        {
            lock (_list)
            {
                TcpConnectionHolder[] list = new TcpConnectionHolder[_list.Count];
                _list.Values.CopyTo(list, 0);
                return list;
            }
        }

        internal TcpConnectionHolder GetLocalConnection(byte connectionId)
        {
            TcpConnectionHolder conn;
            if (_list.TryGetValue(connectionId, out conn))
            {
                return conn;
            }
            else
            {
                throw new ConnectionException("Connection id " + connectionId + " unknown");
            }
        }

        internal TcpConnectionHolder GetRemoteConnection(byte remoteConnectionId)
        {
            lock (_list)
            {
                byte localConnectionId;
                if (_remoteLocalIdMap.TryGetValue(remoteConnectionId, out localConnectionId))
                {
                    return GetLocalConnection(localConnectionId);
                }
                else
                {
                    throw new ConnectionException("No local connection id found for remote connection [" + remoteConnectionId + "]");
                }
            }
        }

        internal bool IsRemoteConnection(byte remoteConnectionId)
        {
            return _remoteLocalIdMap.ContainsKey(remoteConnectionId);
        }

        internal bool IsLocalConnection(byte localConnectionId)
        {
            return _list.ContainsKey(localConnectionId);
        }

    }
}
