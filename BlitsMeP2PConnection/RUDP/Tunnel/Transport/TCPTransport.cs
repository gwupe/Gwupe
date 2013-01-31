using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    public class TCPTransport : ITransport
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (TCPTransport));
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

        public ITransportManager TransportManager { get { return _transportManager; } }

        public TCPTransport(ITransportManager transportManager)
        {
            _transportManager = transportManager;
            _activeTCPConnections = new TcpConnectionList();
            _listeningNamedTCPEndPoints = new Dictionary<String, NamedTCPListener>();
            _tcpConnectionHelper = new TCPConnectionHelper();
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
                    TcpConnectionHolder discHolder = _activeTCPConnections.GetRemoteConnection(packet.ConnectionId);
                    // this closes everything, socket, connection and removes it from the list.
                    discHolder.Connection.Close();
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to close connection [remote id = " + packet.ConnectionId + "] : " + e.Message);
                }
            }
        }

        private void ProcessDataPacket(StandardTcpDataPacket packet)
        {
            try
            {
                TcpConnectionHolder conn = _activeTCPConnections.GetRemoteConnection(packet.ConnectionId);
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
            }
            catch (ConnectionException e)
            {
                Logger.Error("Dropping connect rs ack packet [" + packet + "], failed to get a connection : " + e.Message);
            }
            try
            {
                conn.Connection.Open();
            }
            catch (Exception e)
            {
                Logger.Error("Error starting connection [" + conn.ConnectionId + "] : " + e.Message);
                if (conn != null)
                {
                    conn.Connection.Close();
                }
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
                    response.remoteConnectionId = _activeTCPConnections.GetRemoteConnection(packet.ConnectionId).ConnectionId;
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

                            TcpOne4OneConnection connection = new TcpOne4OneConnection(this, localConnectionId);
                            // only add the connection if 
                            if (response.success = _listeningNamedTCPEndPoints[packet.connectionName].connectCallback(connection.socket))
                            {
                                _activeTCPConnections.Add(localConnectionId, packet.ConnectionId, connection);
                                response.remoteConnectionId = localConnectionId;
                            }
                        }
                        else
                        {
#if(DEBUG)
                            Logger.Debug("No listener for " + packet.connectionName);
#endif
                            response.success = false;
                        }
                    }
                }
            }
#if(DEBUG)
            Logger.Debug("Sending connection response : " + response);
#endif
            _transportManager.SendData(response);
        }

        #endregion

        public void SendData(ITcpPacket packet)
        {
            _transportManager.SendData(packet);
        }

        #region Connection Handling

        public ITcpOverUdptSocket OpenConnection(String endPoint)
        {
            lock (_connectLock)
            {
                byte connectionId = GetNextConnectionId();
                byte remoteConnectionId = _tcpConnectionHelper.ConnectNamed(connectionId, endPoint, 10000, this);
#if(DEBUG)
                Logger.Debug("Successfully connected to service " + endPoint + " local connection [" + connectionId + "], remote connection [" + remoteConnectionId + "]");
#endif
                ITcpConnection connection = new TcpOne4OneConnection(this, connectionId);
                _activeTCPConnections.Add(connectionId, remoteConnectionId, connection);
                _tcpConnectionHelper.AckConnectionResponse(connectionId, this);
                connection.Open();
                return connection.socket;
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
                _activeTCPConnections.Remove(connectionId);
                _tcpConnectionHelper.CloseConnection(connectionId, this);
            }
        }

        #endregion

        public void Close(bool initiatedBySelf)
        {
            foreach (TcpConnectionHolder tcpConnectionHolder in _activeTCPConnections.GetList())
            {
                CloseConnection(tcpConnectionHolder.ConnectionId);
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
        internal ITcpConnection Connection;
        internal Object SendLock;
        internal Object ReceiveLock;
        internal byte RemoteConnectionId;
        internal byte ConnectionId;

        internal TcpConnectionHolder(ITcpConnection conn, byte connectionId, byte remoteConnectionId)
        {
            Connection = conn;
            RemoteConnectionId = remoteConnectionId;
            ConnectionId = connectionId;
            SendLock = new Object();
            ReceiveLock = new Object();
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

        internal void Add(byte localConnectionId, byte remoteConnectionId, ITcpConnection connection)
        {
            lock (_list)
            {
                _list.Add(localConnectionId, new TcpConnectionHolder(connection, localConnectionId, remoteConnectionId));
                _remoteLocalIdMap.Add(remoteConnectionId, localConnectionId);
            }
        }

        internal bool Remove(byte localConnectionId)
        {
            lock (_list)
            {
                TcpConnectionHolder holder;
                if (_list.TryGetValue(localConnectionId, out holder))
                {
                    _list.Remove(localConnectionId);
                    if (_remoteLocalIdMap.ContainsKey(holder.RemoteConnectionId))
                    {
                        _remoteLocalIdMap.Remove(holder.RemoteConnectionId);
                    }
                }
                else
                {
                    return false;
                }
            }
            return true;
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
