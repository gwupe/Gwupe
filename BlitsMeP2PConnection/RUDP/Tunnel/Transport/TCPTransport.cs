using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly TcpConnectionList _tcpConnections; // All the Active Connections
        private readonly ITransportManager _transportManager;

        private byte _lastConnectionId = byte.MaxValue;

        // Locks for various activities
        private readonly object _connectLock = new Object();
        private readonly object _disconnectLock = new Object();
        private readonly object _receiveConnectLock = new Object();
        private readonly object _lastConnectionLock = new Object();
        private bool _isDisconnecting;
        private Random _rand;
        private const byte MaxSupportedProtocolId = 2;

        public ITransportManager TransportManager { get { return _transportManager; } }

        public TCPTransport(ITransportManager transportManager)
        {
            _transportManager = transportManager;
            transportManager.Inactive += delegate { CloseConnections(); };
            _tcpConnections = new TcpConnectionList();
            _listeningNamedTCPEndPoints = new Dictionary<String, NamedTCPListener>();
            _tcpConnectionHelper = new TCPConnectionHelper(this);
            _rand = new Random();
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
                    case BasicTcpPacket.PKT_TYPE_DISCONNECT_ACK:
                        _tcpConnectionHelper.ProcessDisconnectAck((StandardDisconnectAckPacket)packet);
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
            // immediately acknowlege that we got it (with remote connection id as the connection id)
            SendData(new StandardDisconnectAckPacket(packet.ConnectionId));
            // if we are not disconnecting, start the process.
            try
            {
                // do we have it in our list?
                TcpConnectionHolder connectionHolder = _tcpConnections.GetRemoteConnection(packet.ConnectionId);
                // this method will send the disconnectRs packet
                connectionHolder.Connection.ProcessDisconnect(packet);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to close connection [remote id = " + packet.ConnectionId + "] : " + e.Message);
            }
        }

        private void ProcessDataPacket(StandardTcpDataPacket packet)
        {
            try
            {
                TcpConnectionHolder conn = _tcpConnections.GetRemoteConnection(packet.ConnectionId);
                // If the connect response ack packet was lost, its possible that this connection is not open, we need to open it now
                if (!conn.Connection.Established)
                {
                    Logger.Debug("Outgoing Connection [" + conn.Connection.ConnectionId + "] received data, means our connect rs ack was lost, auto opening");
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
                TcpConnectionHolder conn = _tcpConnections.GetRemoteConnection(packet.ConnectionId);
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
                conn = _tcpConnections.GetRemoteConnection(packet.ConnectionId);
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
            StandardNamedConnectRsPacket response = new StandardNamedConnectRsPacket
                {ConnectionId = packet.ConnectionId};
            // By default the conn id going back is the conn id of the request
            // this will change if the connection is a success, then it will be our conn id.
            lock (_receiveConnectLock)
            {
                // If this connection exists
                if (_tcpConnections.IsRemoteConnection(packet.ConnectionId))
                {
                    Logger.Debug("Request for connection id [" + packet.ConnectionId + "] already established, resending response");
                    TcpConnectionHolder tcpConnectionHolder = _tcpConnections.GetRemoteConnection(packet.ConnectionId);
                    response.RemoteConnectionId = tcpConnectionHolder.Connection.ConnectionId;
                    response.ProtocolId = tcpConnectionHolder.Connection.ProtocolId;
                    response.Sequence = tcpConnectionHolder.Connection.NextSeqToSend;
                    // we can respond that we are already established here
                    response.Success = true;
                }
                else
                {
                    lock (_listeningNamedTCPEndPoints)
                    {
                        if (_listeningNamedTCPEndPoints.ContainsKey(packet.ConnectionName))
                        {
                            byte localConnectionId = GetNextConnectionId();
                            Logger.Debug("Found a listener for " + packet.ConnectionName + " connecting local conn [" + localConnectionId + "] to remote conn [" + packet.ConnectionId + "]");
                            byte agreedProtocolId = packet.ProtocolId;
                            if (packet.ProtocolId > MaxSupportedProtocolId)
                            {
                                Logger.Debug("They wanted protocol " + packet.ProtocolId + ", but I only support up to " + MaxSupportedProtocolId + ", so I propose it.");
                                agreedProtocolId = MaxSupportedProtocolId;
                            }
                            Logger.Debug("I have agreed to protocol " + agreedProtocolId);

                            response.ProtocolId = agreedProtocolId;
                            response.Sequence = (ushort) (_rand.Next(ushort.MaxValue));
                            ITcpTransportLayer connection = null;
                            switch (agreedProtocolId)
                            {
                                case (2):
                                    {
                                        connection = new TcpTransportLayerSlidingWindow(this, localConnectionId, packet.ConnectionId, response.Sequence, packet.Sequence);
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
                                response.Success =
                                    _listeningNamedTCPEndPoints[packet.ConnectionName].ConnectCallback(connection.Socket);
                                if (response.Success)
                                {
                                    // set it up to tie up loose ends when the connection dies
                                    connection.ConnectionClose += ConnectionOnConnectionClose;
                                    _tcpConnections.Add(connection);
                                    response.RemoteConnectionId = localConnectionId;
                                }
                            }
                            else
                            {
                                response.Success = false;
                            }
                        }
                        else
                        {
                            Logger.Warn("No listener for " + packet.ConnectionName);
                            response.Success = false;
                        }
                    }
                }
            }
            Logger.Debug("Sending connection response : " + response);
            SendData(response);
            if (response.Success)
            {
                // This sends a positive response, we would like to get an Ack of this connection response
                // which will open this connection, but should that ack not arrive, let set a timer to automatically
                // open this connection after 10 seconds
                // we know that the sender will retry the connection should it not receive our response.
                _tcpConnections.GetLocalOpenConnection(response.RemoteConnectionId).StartTimerToOpen();
            }
        }

        #endregion

        #region Sending

        public void SendData(ITcpPacket packet)
        {
            if (_transportManager.IsActive)
            {
                _transportManager.SendData(packet);
#if DEBUG
                Logger.Debug("Sent packet : " + packet);
#endif
            }
            else
            {
                throw new IOException("Transport manager is inactive (has no tunnels), cannot send data.");
            }
        }

        #endregion Sending

        #region Connection Handling

        public ITcpOverUdptSocket OpenConnection(string endPoint)
        {
            return OpenConnection(endPoint, MaxSupportedProtocolId);
        }

        public ITcpOverUdptSocket OpenConnection(String endPoint, byte protocolId)
        {
            lock (_connectLock)
            {
                byte connectionId = GetNextConnectionId();
                // this method will send the necessary packet requesting connection, handle the response
                ITcpTransportLayer connection = _tcpConnectionHelper.ConnectNamed(connectionId, endPoint, 10000, protocolId);
                Logger.Debug("Successfully connected to service " + endPoint + " local connection [" + connectionId + "], remote connection [" + connection.RemoteConnectionId + "]");
                // add the connection to our list of active connections
                _tcpConnections.Add(connection);
                // notify remote endpoint we are ready to accept and send data on this connection
                SendData(new StandardConnectRsAckPacket(connectionId));
                // set it up to tie up loose ends when the connection dies
                connection.ConnectionClose += ConnectionOnConnectionClose;
                // notify the connection it can start
                connection.Open();
                // return the socket to upstream so it can start reading/writing
                return connection.Socket;
            }
        }

        private byte GetNextConnectionId()
        {
            lock (_lastConnectionLock)
            {
                byte saveLast = _lastConnectionId;
                while (_tcpConnections.IsLocalOpenConnection(++_lastConnectionId))
                {
                    if (_lastConnectionId != saveLast)
                    {
                        throw new ConnectionException("Cannot get next connection id, all connections in use");
                    }
                }
            }
            Logger.Debug("Found next connection id " + _lastConnectionId);
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
            Logger.Debug("Listening for " + endPoint);
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
            // if this is id is in our list of local connections
            if (_tcpConnections.IsLocalOpenConnection(connectionId))
            {
                // we close the connection (if its in the process of being closed, this will do nothing)
                _tcpConnections.GetLocalOpenConnection(connectionId).Connection.Close();
            }
        }

        private void CloseConnections()
        {
            foreach (TcpConnectionHolder tcpConnectionHolder in _tcpConnections.GetActiveConnectionList())
            {
                CloseConnection(tcpConnectionHolder.Connection.ConnectionId);
            }
        }

        // this is called when a connection closes
        private void ConnectionOnConnectionClose(object sender, EventArgs eventArgs)
        {
            ITcpTransportLayer connection = (ITcpTransportLayer)sender;
            if (!connection.Disconnected)
            {
                // Send the disconnect packet sequence (-> dis <- ack <- dis_rs)
                _tcpConnectionHelper.DisconnectConnection(connection.ConnectionId, connection.LastSeqSent);
            } else
            {
                SendData(new StandardDisconnectRsPacket(connection.RemoteConnectionId));
            }
            // remove the connection from our list of open connections
            _tcpConnections.Remove(connection.ConnectionId);
        }


        #endregion

        public void Close()
        {
            if (!Closing && !Closed)
            {
                Closing = true;
                CloseConnections();
                _listeningNamedTCPEndPoints.Clear();
                Closing = false;
                Closed = true;
            }
        }

        protected bool Closed { get; private set; }

        protected bool Closing { get; private set; }

    }

    internal class NamedTCPListener
    {
        internal String ListenerName;
        internal Func<ITcpOverUdptSocket, bool> ConnectCallback;

        internal NamedTCPListener(String name, Func<ITcpOverUdptSocket, bool> callback)
        {
            this.ListenerName = name;
            this.ConnectCallback = callback;
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
                        Logger.Debug("Connection timer for " + conn.ConnectionId + " elapsed, auto opening connection " + conn.ConnectionId);
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

    /* This class is for keeping a list of current and closing tcp connections 
     * as well as providing a lookup function
     */

    internal class TcpConnectionList
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TcpConnectionList));
        private readonly Dictionary<byte, TcpConnectionHolder> _localOpenConnectionList;
        private readonly Dictionary<byte, byte> _remoteLocalIdMap;
        private readonly Object _connectionLock;

        internal TcpConnectionList()
        {
            _localOpenConnectionList = new Dictionary<byte, TcpConnectionHolder>();
            _remoteLocalIdMap = new Dictionary<byte, byte>();
            _connectionLock = new object();
        }

        internal void Add(ITcpTransportLayer connection)
        {
            lock (_connectionLock)
            {
                _localOpenConnectionList.Add(connection.ConnectionId, new TcpConnectionHolder(connection));
                _remoteLocalIdMap.Add(connection.RemoteConnectionId, connection.ConnectionId);
            }
        }

        internal TcpConnectionHolder Remove(byte localConnectionId)
        {
            TcpConnectionHolder holder = null;
            lock (_connectionLock)
            {
                if (_localOpenConnectionList.TryGetValue(localConnectionId, out holder))
                {
                    _localOpenConnectionList.Remove(localConnectionId);
                    if (_remoteLocalIdMap.ContainsKey(holder.Connection.RemoteConnectionId))
                    {
                        _remoteLocalIdMap.Remove(holder.Connection.RemoteConnectionId);
                    }
                    Logger.Debug("Removed connection " + localConnectionId + " to from lists.");
                }
            }
            return holder;
        }

        internal TcpConnectionHolder[] GetActiveConnectionList()
        {
            lock (_connectionLock)
            {
                TcpConnectionHolder[] list = new TcpConnectionHolder[_localOpenConnectionList.Count];
                _localOpenConnectionList.Values.CopyTo(list, 0);
                return list;
            }
        }

        internal TcpConnectionHolder GetLocalOpenConnection(byte connectionId)
        {
            TcpConnectionHolder conn;
            if (_localOpenConnectionList.TryGetValue(connectionId, out conn))
            {
                return conn;
            }
            throw new ConnectionException("Connection id " + connectionId + " unknown in open connections list");
        }

        internal TcpConnectionHolder GetRemoteConnection(byte remoteConnectionId)
        {
            lock (_connectionLock)
            {
                byte localConnectionId;
                if (_remoteLocalIdMap.TryGetValue(remoteConnectionId, out localConnectionId))
                {
                    if (_localOpenConnectionList.ContainsKey(localConnectionId))
                    {
                        return GetLocalOpenConnection(localConnectionId);
                    }
                }
                throw new ConnectionException("No local connection id found for remote connection [" + remoteConnectionId + "]");
            }
        }

        internal bool IsRemoteConnection(byte remoteConnectionId)
        {
            return _remoteLocalIdMap.ContainsKey(remoteConnectionId);
        }

        internal bool IsLocalOpenConnection(byte localConnectionId)
        {
            return _localOpenConnectionList.ContainsKey(localConnectionId);
        }
    }
}
