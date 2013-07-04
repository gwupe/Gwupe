using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlitsMe.Communication.P2P.RUDP.Packet.API;
using BlitsMe.Communication.P2P.RUDP.Packet.TCP;
using BlitsMe.Communication.P2P.RUDP.Socket.API;
using BlitsMe.Communication.P2P.RUDP.Socket;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;
using System.Threading;
using BlitsMe.Communication.P2P.RUDP.Tunnel.Transport;
using log4net;
using BlitsMe.Communication.P2P.Exceptions;

namespace BlitsMe.Communication.P2P.RUDP.Tunnel
{
    public class TcpTransportLayerOne4One : TcpTransportLayer
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TcpTransportLayerOne4One));

        // Event Handlers
        private readonly AutoResetEvent _ackEvent = new AutoResetEvent(false); // when ack comes in, this gets raised
        private ushort _sequenceIn = ushort.MaxValue - 50; // sequence number we last received in
        private ushort _sequenceOut = ushort.MaxValue - 50; // sequence number we last sent out
        private readonly Object _sendingLock = new Object(); // lock to make sending thread safe
        private ushort _lastSeqSent;
        private bool _disconnected;
        private ushort _nextSeqToSend;
        public int AckWaitInterval { get; private set; }
        public override byte ProtocolId { get { return 1; } }

        public int PacketCountReceiveAckValid { get; private set; }
        public int PacketCountReceiveAckInvalid { get; private set; }
        public int PacketCountReceiveDataFirst { get; private set; }
        public int PacketCountReceiveDataResend { get; private set; }
        public int PacketCountTransmitAckFirst { get; private set; }
        public int PacketCountTransmitAckResend { get; private set; }
        public int PacketCountTransmitDataFirst { get; private set; }
        public int PacketCountTransmitDataResend { get; private set; }

        public TcpTransportLayerOne4One(ITCPTransport transport, byte connectionId, byte remoteConnectionId, ushort firstLocalSequence, ushort firstRemoteSequence)
            : base(transport, connectionId, remoteConnectionId)
        {
            AckWaitInterval = 300;
        }

        public override void SendData(byte[] data, int length, int timeout)
        {
            BlockIfNotEstablished(timeout);
            long waitTime = timeout * 10000;
            lock (_sendingLock)
            {
                _sequenceOut++;
                StandardTcpDataPacket packet = new StandardTcpDataPacket(_sequenceOut);
                byte[] packetData = new byte[length];
                Array.Copy(data,packetData,length);
                packet.Data = packetData;
                packet.ConnectionId = ConnectionId;
#if(DEBUG)
                Logger.Debug("Sending packet " + packet.ToString());
#endif
                long startTime = DateTime.Now.Ticks;
                _ackEvent.Reset();
                do
                {
                    if (!Established)
                    {
#if(DEBUG)
                        Logger.Debug("Connection is down, aborting data send");
#endif
                        throw new ConnectionException("Cannot send data, connection is down");
                    }
                    Transport.SendData(packet);
                    if (packet.ResendCount > 0) { PacketCountTransmitDataResend++; }
                    else { PacketCountTransmitDataFirst++; }
                    if (DateTime.Now.Ticks - startTime > waitTime)
                    {
#if(DEBUG)
                        Logger.Debug("Data timeout : " + (DateTime.Now.Ticks - startTime));
#endif
                        _sequenceOut--;
                        throw new TimeoutException("Timeout occured while sending data to " + Transport.TransportManager.RemoteIp);
                    }
#if(DEBUG)
                    Logger.Debug("Waiting for ack from " + Transport.TransportManager.RemoteIp + " for packet " + _sequenceOut);
#endif
                    packet.ResendCount++;

                } while (!_ackEvent.WaitOne(AckWaitInterval + (AckWaitInterval * packet.ResendCount)));
                long stopTime = DateTime.Now.Ticks;
                int ackTrip = (int)((stopTime - startTime) / 10000);
                if (ackTrip > AckWaitInterval && (ackTrip - AckWaitInterval) > 20)
                {
                    // ackTrip was more than 50ms longer than our interval, we need to adjust up
                    AckWaitInterval = ((ackTrip - AckWaitInterval) / 2) + AckWaitInterval;
#if(DEBUG)
                    Logger.Debug("Adjusted ack wait interval UP to " + AckWaitInterval + ", trip was " + ackTrip);
#endif
                }
                else if (ackTrip < AckWaitInterval && (AckWaitInterval - ackTrip) > 20)
                {
                    // ackTrip was more than 50ms shorter than our internal, we need to adjust down
                    AckWaitInterval = AckWaitInterval - ((AckWaitInterval - ackTrip) / 2);
#if(DEBUG)
                    Logger.Debug("Adjusted ack wait interval DOWN to " + AckWaitInterval + ", trip was " + ackTrip);
#endif
                }
            }
        }


        public override ushort LastSeqSent
        {
            get { return _lastSeqSent; }
        }

        public override ushort NextSeqToSend
        {
            get { return _nextSeqToSend; }
        }

        public override void ProcessDataPacket(ITcpDataPacket packet)
        {
            if (BasicTcpPacket.CompareSequences((ushort)(_sequenceIn + 1), packet.Sequence) == 0)
            {
                // This is exactly what we were expecting
                _sequenceIn++;
                if (packet.ResendCount > 0) { PacketCountReceiveDataResend++; }
                else { PacketCountReceiveDataFirst++; }
                SendAck(packet, true);
                Socket.BufferClientData(packet.Data);
            }
            else if (BasicTcpPacket.CompareSequences((ushort)(_sequenceIn + 1), packet.Sequence) > 0)
            {
                // This is an old packet, don't process (already did that), just send ack
#if(DEBUG)
                Logger.Debug("Got old Data packet, sending ACK, data already processed.");
#endif
                SendAck(packet, false);
            }
            else
            {
#if(DEBUG)
                Logger.Debug("Got unexpected sequence (expected " + _sequenceIn + ") from packet " + packet);
#endif
            }
        }

        private void SendAck(ITcpPacket packet, bool firstSend)
        {
            StandardAckPacket outPacket = new StandardAckPacket(packet.Sequence);
            outPacket.ConnectionId = ConnectionId;
            try
            {
                Transport.SendData(outPacket);
#if DEBUG
                Logger.Debug("Sent ack [" + outPacket + "]");
#endif

                if (firstSend) { PacketCountTransmitAckFirst++; }
                else { PacketCountTransmitAckResend++; }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to send ack to peer for packet " + packet.Sequence + " : " + e.Message);
            }

        }

        public override void ProcessAck(StandardAckPacket packet)
        {
            if (packet.Sequence == _sequenceOut)
            {
                PacketCountReceiveAckValid++;
                _ackEvent.Set();
            }
            else
            {
                PacketCountReceiveAckInvalid++;
#if DEBUG
                Logger.Debug("Dropping unexpected ack : " + packet);
#endif

            }
        }

        public override void ProcessDisconnect(StandardDisconnectPacket packet)
        {
            throw new NotImplementedException();
        }

        public override bool Disconnected
        {
            get { return _disconnected; }
        }

        public override void Close()
        {
            
        }
    }
}
