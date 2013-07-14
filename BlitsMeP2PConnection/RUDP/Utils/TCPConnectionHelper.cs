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
        private readonly Dictionary<byte, StandardDisconnectAckPacket> _disconnectAcks = new Dictionary<byte, StandardDisconnectAckPacket>();
        private readonly Random _rand;

        public TCPConnectionHelper(TCPTransport transport)
        {
            _transport = transport;
            _rand = new Random();
        }

        public ITcpTransportLayer ConnectNamed(byte connectionId, String namedEndPoint, int timeout, byte protocolId)
        {
            long waitTime = timeout;
            Logger.Debug("Connecting to " + namedEndPoint + ", timeout is " + waitTime + "ms");
            StandardNamedConnectRqPacket packet = new StandardNamedConnectRqPacket(connectionId)
                {
                    ConnectionName = namedEndPoint,
                    ProtocolId = protocolId,
                    Sequence = (ushort)(_rand.Next(ushort.MaxValue))
                };
            long startTime = DateTime.Now.Ticks;
            _connectEvents.Add(connectionId,new AutoResetEvent(false));
            do
            {
                _transport.SendData(packet);
                packet.ResendCount++;
                var timeSpan = new TimeSpan(DateTime.Now.Ticks - startTime);
                if (timeSpan.TotalMilliseconds > waitTime)
                {
                    Logger.Debug("Connect timeout : " + timeSpan.TotalMilliseconds + "ms");
                    if (_connectEvents.ContainsKey(connectionId)) _connectEvents.Remove(connectionId);
                    throw new TimeoutException("Timeout occured while connecting to " + namedEndPoint);
                }
                Logger.Debug("Waiting for connect response from " + namedEndPoint);
            } while (_connectEvents.ContainsKey(connectionId) && !_connectEvents[connectionId].WaitOne(5000));

            try
            {
                var response = _connectResults[connectionId];
                if (response.Success)
                {
                    ITcpTransportLayer connection;
                    switch (response.ProtocolId)
                    {
                        case (2):
                            {
                                connection = new TcpTransportLayerSlidingWindow(_transport, connectionId, response.RemoteConnectionId, packet.Sequence, response.Sequence);
                                break;
                            }
                        default:
                            {
                                throw new ConnectionException("Failed to agree on a protocol, I don't support " +
                                                              response.RemoteConnectionId);
                            }
                    }
                    Logger.Debug("We have agreed to protocol " + response.ProtocolId);
                    
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
            Logger.Debug("Processing Connect Response " + packet);
            // In a connection response, ConnectionId is set to the same as the connection request ConnectionId
            if (_connectEvents.ContainsKey(packet.ConnectionId))
            {
                _connectResults[packet.ConnectionId] = packet;
                _connectEvents[packet.ConnectionId].Set();
                _connectEvents.Remove(packet.ConnectionId);
            } else
            {
                // we know nothing about this (probably timed out), close this connection
                Logger.Error("Got a Connect Response with an Id we don't have [" + packet.ConnectionId + "]");
                //CloseConnection(packet.ConnectionId, packet.Sequence);
            }
        }

        public void DisconnectConnection(byte connectionId, ushort seq)
        {
            const long waitTime = 120000; // 2 minute disconnect time
            const int disconnectRetryTimeout = 20000;
            Logger.Debug("Disconnecting tcp session " + connectionId);
            try
            {
                var packet = new StandardDisconnectPacket(connectionId);
                packet.Sequence = seq;
                long startTime = DateTime.Now.Ticks;
                _disconnectEvents[connectionId] = new AutoResetEvent(false);
                do
                {
                    // we only do this if we haven't received the ack of our disconnect
                    if (!_disconnectAcks.ContainsKey(connectionId))
                    {
                        _transport.SendData(packet);
                        packet.ResendCount++;
                    }
                    else
                    {
                        Logger.Debug("Will not resend disconnect request, remote side has acked it already");
                    }
                    var timeSpan = new TimeSpan(DateTime.Now.Ticks - startTime);
                    if (timeSpan.TotalMilliseconds > waitTime)
                    {
                        Logger.Debug("Disconnect timeout : " + timeSpan.TotalMilliseconds + "ms");
                        break;
                    }
                    Logger.Debug("Waiting for disconnect response for connection id " + connectionId);
                } while (_disconnectEvents.ContainsKey(connectionId) && !_disconnectEvents[connectionId].WaitOne(disconnectRetryTimeout));

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
            if (_disconnectAcks.ContainsKey(connectionId)) _disconnectAcks.Remove(connectionId);
        }

        public void ProcessDisconnectAck(StandardDisconnectAckPacket packet)
        {
            // only process if we are waiting for one
            if (_disconnectEvents.ContainsKey(packet.ConnectionId))
            {
                if (!_disconnectAcks.ContainsKey(packet.ConnectionId))
                {
                    _disconnectAcks.Add(packet.ConnectionId, packet);
                }
            }
        }

        public void ProcessDisconnectRs(StandardDisconnectRsPacket packet)
        {
            Logger.Debug("Processing Disconnect Response " + packet);
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
    }
}
