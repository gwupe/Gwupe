using System;
using System.Collections.Generic;
using System.Threading;
using BlitsMe.Communication.P2P.RUDP.Packet.TCP;
using BlitsMe.Communication.P2P.RUDP.Tunnel;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;
using BlitsMe.Communication.P2P.Exceptions;
using BlitsMe.Communication.P2P.RUDP.Tunnel.Transport;
using log4net;

namespace BlitsMe.Communication.P2P.RUDP.Utils
{
    internal class TCPConnectionHelper
    {
        private readonly TCPTransport _transport;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TCPConnectionHelper));
        private readonly Dictionary<byte,AutoResetEvent> _connectEvents = new Dictionary<byte, AutoResetEvent>();
        private readonly Dictionary<byte,AutoResetEvent> _disconnectEvents = new Dictionary<byte, AutoResetEvent>();
        private readonly Dictionary<byte, StandardNamedConnectRsPacket> _connectResults = new Dictionary<byte, StandardNamedConnectRsPacket>();

        public TCPConnectionHelper(TCPTransport transport)
        {
            _transport = transport;
        }

        public ITcpTransportLayer ConnectNamed(byte connectionId, String namedEndPoint, int timeout, byte protocolId)
        {
            long waitTime = timeout;
#if(DEBUG)
            Logger.Debug("Connecting to " + namedEndPoint + ", timeout is " + waitTime + "ms");
#endif
            StandardNamedConnectRqPacket packet = new StandardNamedConnectRqPacket(connectionId)
                {
                    connectionName = namedEndPoint,
                    protocolId = protocolId,
                };
            long startTime = DateTime.Now.Ticks;
            _connectEvents.Add(connectionId,new AutoResetEvent(false));
            do
            {
#if(DEBUG)
                Logger.Debug("Sending connect packet : " + packet);
#endif
                _transport.SendData(packet);
                var timeSpan = new TimeSpan(DateTime.Now.Ticks - startTime);
                if (timeSpan.TotalMilliseconds > waitTime)
                {
#if(DEBUG)
                    Logger.Debug("Connect timeout : " + timeSpan.TotalMilliseconds + "ms");
#endif
                    if (_connectEvents.ContainsKey(connectionId)) _connectEvents.Remove(connectionId);
                    throw new TimeoutException("Timeout occured while connecting to " + namedEndPoint);
                }
#if(DEBUG)
                Logger.Debug("Waiting for connect response from " + namedEndPoint);
#endif
            } while (_connectEvents.ContainsKey(connectionId) && !_connectEvents[connectionId].WaitOne(5000));

            try
            {
                var response = _connectResults[connectionId];
                if (response.success)
                {
                    ITcpTransportLayer connection;
                    switch (response.protocolId)
                    {
                        case (1):
                            {
                                connection = new TcpTransportLayerOne4One(_transport, connectionId, response.remoteConnectionId);
                                break;
                            }
                        case (2):
                            {
                                connection = new TcpTransportLayerSlidingWindow(_transport, connectionId,
                                                                                response.remoteConnectionId);
                                break;
                            }
                        default:
                            {
                                throw new ConnectionException("Failed to agree on a protocol, I don't support " +
                                                              response.remoteConnectionId);
                            }
                    }
#if(DEBUG)
                    Logger.Debug("We have agreed to protocol " + response.protocolId);
#endif
                    
                    return connection;
                }
                throw new ConnectionException("Failed to connect to " + namedEndPoint + ", service is unavailable");
            }
            finally
            {
                _connectResults.Remove(connectionId);
            }
        }

        public void ProcessConnectRs(StandardNamedConnectRsPacket packet)
        {
#if(DEBUG)
            Logger.Debug("Processing Connect Response " + packet);
#endif
            // In a connection response, ConnectionId is set to the same as the connection request ConnectionId
            if (_connectEvents.ContainsKey(packet.ConnectionId))
            {
                _connectResults[packet.ConnectionId] = packet;
                _connectEvents[packet.ConnectionId].Set();
                _connectEvents.Remove(packet.ConnectionId);
            } else
            {
                // we know nothing about this (probably timed out), close this connection
                Logger.Warn("Got a Connect Response with an Id we don't have [" + packet.ConnectionId + "], closing it");
                CloseConnection(packet.ConnectionId);
            }
        }

        public void CloseConnection(byte connectionId)
        {
            long waitTime = 120000; // 2 minute disconnect time
#if(DEBUG)
            Logger.Debug("Disconnecting tcp session " + connectionId);
#endif
            try
            {
                var packet = new StandardDisconnectPacket(connectionId);
                long startTime = DateTime.Now.Ticks;
                _disconnectEvents[connectionId] = new AutoResetEvent(false);
                do
                {
#if(DEBUG)
                    Logger.Debug("Sending disconnect packet : " + packet);
#endif
                    _transport.SendData(packet);
                    packet.ResendCount++;
                    var timeSpan = new TimeSpan(DateTime.Now.Ticks - startTime);
                    if (timeSpan.TotalMilliseconds > waitTime)
                    {
#if(DEBUG)
                        Logger.Debug("Disconnect timeout : " + timeSpan.TotalMilliseconds + "ms");
#endif
                        break;
                    }
#if(DEBUG)
                    Logger.Debug("Waiting for disconnect response for connection id " + connectionId);
#endif
                } while (_disconnectEvents.ContainsKey(connectionId) && !_disconnectEvents[connectionId].WaitOne(20000));

                if(_disconnectEvents.ContainsKey(connectionId))
                {
                    // this means the disconnect wasn't acknowledged, oh well!
                    Logger.Warn("Failed to receive a disconnect response for connection id " + connectionId + ", closing regardless.");
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to send the close packet, closing connection anyway. : " + e.Message, e);
            }
            if (_disconnectEvents.ContainsKey(connectionId)) _disconnectEvents.Remove(connectionId);
        }

        public void ProcessDisconnectRs(StandardDisconnectRsPacket packet)
        {
#if(DEBUG)
            Logger.Debug("Processing Disconnect Response " + packet);
#endif
            // In a connection response, ConnectionId is set to the same as the connection request ConnectionId
            if (_disconnectEvents.ContainsKey(packet.ConnectionId))
            {
                AutoResetEvent disconnectEvent = _disconnectEvents[packet.ConnectionId];
                _disconnectEvents.Remove(packet.ConnectionId);
                disconnectEvent.Set();
            }
            else
            {
                // we know nothing about this (probably timed out), close this connection
                Logger.Warn("Got a disconnect Response with an Id we don't have [" + packet.ConnectionId + "], ignoring");
            }
        }

        public void AckConnectionResponse(byte connectionId)
        {
#if(DEBUG)
            Logger.Debug("Acking tcp connection " + connectionId);
#endif
            _transport.SendData(new StandardConnectRsAckPacket(connectionId));
        }
    }
}
