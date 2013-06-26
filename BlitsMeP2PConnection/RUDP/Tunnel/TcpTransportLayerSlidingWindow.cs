using System;
using System.IO;
using System.Threading;
using System.Timers;
using BlitsMe.Communication.P2P.Exceptions;
using BlitsMe.Communication.P2P.RUDP.Packet.API;
using BlitsMe.Communication.P2P.RUDP.Packet.TCP;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;
using BlitsMe.Communication.P2P.RUDP.Utils;
using log4net;

namespace BlitsMe.Communication.P2P.RUDP.Tunnel
{
    class TcpTransportLayerSlidingWindow : TcpTransportLayer
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TcpTransportLayerSlidingWindow));
        private const int WindowSize = 32;
        private const double _rtt_alpha = 0.75;
        private const double _rtt_beta = 0.25;
        private const int TcpChunkSize = 1400; // Just below typical mss
        private const int BufferSize = 8192;
        private int _retryInterval = 1000;
        private readonly CircularBuffer<byte> _sendBuffer;
        private readonly ITcpPacket[] _sendWindow;
        private readonly ITcpPacket[] _recvWindow;
        private ITcpPacket _lastAckedPacket;
        private readonly Object _sendingLock = new Object();
        private readonly Object _ackWaitingLock = new Object();
        private ushort _nextDataSeqOut;
        private ushort _nextDataSeqIn;
        private ushort _highestDataSeqIn;
        private byte _sendWindowNextFree;
        private byte _recvWindowsNextFree;
        private byte _oldestUnackedPacket;
        private ushort _srtt = 100;
        private double _deviation = 0;
        private System.Timers.Timer _retryTimer;
        private int _currentTimeout;
        private Thread dataSenderThread;
        public TcpSlidingWindowStats Stats;
        private int _lastAckPacketSeq = 0;
        private int _lastAckPacketCount = 0;
        public override byte ProtocolId { get { return 2; } }

        public TcpTransportLayerSlidingWindow(ITCPTransport transport, byte connectionId, byte remoteConnectionId)
            : base(transport, connectionId, remoteConnectionId)
        {
            _sendWindow = new ITcpPacket[WindowSize];
            _recvWindow = new ITcpPacket[WindowSize];
            _sendBuffer = new CircularBuffer<byte>(BufferSize);
            Stats = new TcpSlidingWindowStats();
            // setup our start seq numbers
            _nextDataSeqOut = _nextDataSeqIn = _sendWindowNextFree = _oldestUnackedPacket = _recvWindowsNextFree = 0;
            _highestDataSeqIn = ushort.MaxValue;
            _retryTimer = new System.Timers.Timer(_retryInterval) { AutoReset = false };
            _retryTimer.Elapsed += delegate { RetryPacketSend(); };
            dataSenderThread = new Thread(DataSender) { IsBackground = true };
            dataSenderThread.Start();
        }

        // Received this data packet
        public override void ProcessDataPacket(ITcpPacket packet)
        {
            if (!Closed)
            {
                Stats.DataPacketsReceived++;
                int compareSequences = BasicTcpPacket.compareSequences(_nextDataSeqIn, packet.Sequence);
                if (compareSequences == 0)
                {
                    Stats.ExpectedDataPacketsReceived++;
                    Stats.ExpectedOverReceivedPercentage = (float) Stats.ExpectedDataPacketsReceived/
                                                           Stats.DataPacketsReceived;
                    if (BasicTcpPacket.compareSequences(packet.Sequence, _highestDataSeqIn) > 0)
                        _highestDataSeqIn = packet.Sequence;
                    // This is exactly what we were expecting
                    // pop it in our list
                    ITcpPacket currentPacket = _recvWindow[_recvWindowsNextFree] = packet;
                    // Now flush our window until we get to a gap
                    while (_recvWindow[_recvWindowsNextFree] != null)
                    {
                        currentPacket = _recvWindow[_recvWindowsNextFree];
                        // Send Data Upstream
                        Socket.BufferClientData(currentPacket.Data);
                        // clear this space in our window
                        _recvWindow[_recvWindowsNextFree] = null;
                        // increment our pointer to next space
                        _recvWindowsNextFree = (byte) ((_recvWindowsNextFree + 1)%WindowSize);
                        // increment our next next expected sequence number
                        _nextDataSeqIn++;
#if DEBUG
                        Logger.Debug("Processed sq=" + currentPacket.Sequence + " upstream, nextDataSeqIn=" +
                                     _nextDataSeqIn + ", recvWindowNextFree=" + _recvWindowsNextFree +
                                     ", highestDataSeqIn=" + _highestDataSeqIn);
#endif
                    }
                    // Send Ack (only when done with sending all the packets we can)
                    SendAck(currentPacket, true);
                    _lastAckedPacket = currentPacket;
                }
                else if (compareSequences > 0)
                {
                    Stats.OldDataPacketsReceived++;
                    if (packet.ResendCount > 0) Stats.OldDataResendPacketsReceived++;
                    Stats.OldOverReceivedPercentage = (float) Stats.OldDataPacketsReceived/Stats.DataPacketsReceived;
                    // This is an old packet, don't process (already did that), just send ack
#if(DEBUG)
                    Logger.Debug("Got old Data packet (resend=" + packet.ResendCount +
                                 "), sending ACK, data already processed.");
#endif
                    SendAck(packet, false);
                }
                else
                {
                    Stats.FutureDataPacketsReceived++;
                    Stats.FutureOverReceivedPercentage = (float) Stats.FutureDataPacketsReceived/
                                                         Stats.DataPacketsReceived;
                    // This is an out of sequence packet from the future, 
                    // if it fits in our window, we need to save it for future use
                    int differenceInSequences = DifferenceInSequences(_nextDataSeqIn, packet.Sequence);
                    if (differenceInSequences < WindowSize)
                    {
                        // ok we can put it in
                        _recvWindow[(_recvWindowsNextFree + differenceInSequences)%WindowSize] = packet;
                        if (BasicTcpPacket.compareSequences(packet.Sequence, _highestDataSeqIn) > 0)
                            _highestDataSeqIn = packet.Sequence;
#if(DEBUG)
                        Logger.Warn("Queued sq=" + packet.Sequence + " in window, nextDataSeqIn=" + _nextDataSeqIn +
                                    ", recvWindowNextFree=" + _recvWindowsNextFree + ", highestDataSeqIn=" +
                                    _highestDataSeqIn);
#endif
                        // Now we send an ack for the last in sequence packet we received
                        if (_lastAckedPacket != null)
                        {
                            SendAck(_lastAckedPacket, false);
                            Stats.HoldingAcksSent++;
                        }
                    }
                    else
                    {
#if(DEBUG)
                        Logger.Warn("Got too far in the future sequence (expected " + _nextDataSeqIn + ") from packet " +
                                    packet + ", dropping");
#endif
                        // Now we send an ack for the last in sequence packet we received
                        if (_lastAckedPacket != null)
                        {
                            SendAck(_lastAckedPacket, false);
                            Stats.HoldingAcksSent++;
                        }
                    }
                }
#if DEBUG
                if (Stats.DataPacketsReceived%1000 == 0)
                {
                    Logger.Debug("Received Stats:\n" + Stats.GetReceiverStats());
                }
#endif
            } else
            {
                Logger.Error("Failed to process data packet, the connection closed.");
            }
        }

        private void SendAck(ITcpPacket packet, bool firstSend)
        {
            StandardAckPacket outPacket = new StandardAckPacket(packet.Sequence) { ConnectionId = ConnectionId, ResendCount = packet.ResendCount };
            try
            {
                Transport.SendData(outPacket);

                if (firstSend)
                    Stats.AcksSent++;
                else
                    Stats.AcksResent++;
#if DEBUG
                Logger.Debug("Sent ack [" + outPacket + "]");
#endif
            }
            catch (Exception e)
            {
                Logger.Error("Failed to send ack to peer for packet " + packet.Sequence + " : " + e.Message);
            }

        }

        public override void SendData(byte[] data, int timeout)
        {
            if (!Closing && !Closed)
            {
                BlockIfNotEstablished(20000);
                _currentTimeout = timeout;
                _sendBuffer.Add(data, timeout);
                Stats.DataPacketBufferCount++;
#if DEBUG
                Logger.Debug("Added " + data.Length + " bytes to the sendBuffer, " + _sendBuffer.Count + " in buffer");
                if (Stats.DataPacketSendCount%1000 == 0)
                {
                    Logger.Debug("Sender Stats:\n" + Stats.GetSenderStats());
                }
#endif
            } else
            {
                throw new IOException("Cannot send data, connection has been closed");
            }
        }

        public void DataSender()
        {
            try
            {
                while (_sendBuffer.Count > 0 || !Closing)
                {
                    long waitStart = Environment.TickCount;
                    lock (_ackWaitingLock)
                    {
                        while (!SpaceInWindow())
                        {
                            Stats.NoSpaceInWindow++;
#if DEBUG
                            Logger.Debug("Pausing send, no space left in window");
#endif
                            if (!Monitor.Wait(_ackWaitingLock, _currentTimeout))
                            {
                                throw new TimeoutException("Timeout occured while sending data to " + Transport.TransportManager.RemoteIp);
                            }
#if DEBUG
                            Logger.Debug("AckWaitLock release, probably space in window now");
#endif
                        }

                    }
                    byte[] payload = _sendBuffer.Get(TcpChunkSize);
                    if (payload.Length > 0)
                    {
#if DEBUG
                        Logger.Debug("Got " + payload.Length + " bytes from the sendBuffer, " + _sendBuffer.Count +
                                     " left in buffer");
#endif
                        StandardTcpDataPacket packet = new StandardTcpDataPacket { Data = payload, ConnectionId = ConnectionId, Sequence = _nextDataSeqOut };
#if(DEBUG)
                        Logger.Debug("Sending packet " + packet);
#endif
                        //long startTime = DateTime.Now.Ticks;
                        if (!Established)
                        {
#if(DEBUG)
                            Logger.Debug("Connection is down, aborting data send");
#endif
                            throw new ConnectionException("Cannot send data, connection is down");
                        }
                        if (_sendWindow[_sendWindowNextFree] != null)
                            throw new ConnectionException(
                                "Cannot insert packet into window, there is something already there! [" +
                                _sendWindow[_sendWindowNextFree].Sequence + "]");
                        Transport.SendData(packet);
                        Stats.AvgSendWaitTime = (Stats.AvgSendWaitTime * Stats.DataPacketSendCount +
                                                 (Environment.TickCount - waitStart)) / (Stats.DataPacketSendCount + 1);
                        Stats.DataPacketSendCount++;
                        packet.Timestamp = Environment.TickCount;
                        _sendWindow[_sendWindowNextFree] = packet;
                        _sendWindowNextFree = (byte)((_sendWindowNextFree + 1) % WindowSize);
                        _nextDataSeqOut++;
                        if (!_retryTimer.Enabled)
                        {
                            _retryTimer.Interval = _retryInterval;
                            _retryTimer.Start();
#if DEBUG
                            Logger.Debug("Setup retry time for " + packet.Sequence + " to " + _retryInterval + "ms");
#endif
                        }
#if DEBUG
                        Logger.Debug("Sent packet " + packet.Sequence + ", nextDataSeqOut=" + _nextDataSeqOut +
                                     ", sendWindowNextFree=" + _sendWindowNextFree + ", oldestUnackedPacket=" +
                                     _oldestUnackedPacket);
#endif
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error("DataSender caught Exception while running buffer : " + e.Message, e);
                if (!Closing)
                    _Close();
            }
        }

        private bool SpaceInWindow()
        {
            // Either the 2 sides of the window are not touching, or they are both pointing to a blank space
            return (_oldestUnackedPacket != _sendWindowNextFree || _sendWindow[_oldestUnackedPacket] == null);
        }

        private int DifferenceInSequences(ushort seq1, ushort seq2)
        {
            if (seq2 < seq1)
            {
                // this means we have wrapped our sequence numbers
                return (seq2 + ushort.MaxValue) - seq1;
            }
            return seq2 - seq1;
        }

        // received this ack
        public override void ProcessAck(StandardAckPacket packet)
        {
            Stats.AcksReceived++;
            lock (_ackWaitingLock)
            {
                // loop through expected acks clearing them until we are in sync with incoming ack packet
                while (_sendWindow[_oldestUnackedPacket] != null)
                {
                    var currentPacket = _sendWindow[_oldestUnackedPacket];
                    int seqDiff = BasicTcpPacket.compareSequences(currentPacket.Sequence,
                                                                  packet.Sequence);
                    // If this packet is the one we are expecting or one down the line (indicating receiver has all those up until this one)
                    if (seqDiff <= 0)
                    {

                        _sendWindow[_oldestUnackedPacket] = null;
                        _oldestUnackedPacket = (byte)((_oldestUnackedPacket + 1) % WindowSize);
#if DEBUG
                        Logger.Debug("Cleared old packet " + currentPacket.Sequence + ", oldestUnackedPacket=" +
                                     _oldestUnackedPacket);
#endif
                        // save the last ack we received (and the count)
                        _lastAckPacketSeq = currentPacket.Sequence;
                        _lastAckPacketCount = 1;
                        if (seqDiff == 0)
                        {
                            // Lets adjust our timeout if this is a real rtt
                            if (packet.ResendCount == 0)
                            {
                                // we can check the RTT here
                                long rtt = Environment.TickCount - currentPacket.Timestamp;
                                AdjustRetryInterval(rtt);
#if DEBUG
                                Logger.Debug("RTT of " + currentPacket.Sequence + " is " + rtt + ", srtt=" + _srtt +
                                             ", deviation=" + _deviation + ", retryInterval=" + _retryInterval);
#endif
                            }
                            // reset the time against the next unacked packet
                            long timeLeft = _retryInterval - (Environment.TickCount - currentPacket.Timestamp);
                            if (timeLeft <= 0) timeLeft = 1;
                            if (_retryTimer.Enabled)
                            {
                                _retryTimer.Stop();
                            }
                            _retryTimer.Interval = timeLeft;
#if DEBUG
                            Logger.Debug("Setup retransmit timeout for " + currentPacket.Sequence + " to " + timeLeft + "ms.");
#endif
                            _retryTimer.Start();
                            break;
                        }
                    }
                    else
                    {
                        // this is an old ack, if we get 3, we retrasmit the next one
                        if (packet.Sequence == _lastAckPacketSeq)
                        {
                            _lastAckPacketCount++;
                            // we only resend via fast transmit if it is a count of 3 and it hasn't been resent before
                            if (_lastAckPacketCount == 3 && currentPacket.ResendCount == 0)
                            {
                                if (_retryTimer.Enabled)
                                {
                                    _retryTimer.Stop();
                                }
                                // fast retransmit
#if DEBUG
                                Logger.Warn("Received 3 acks of seq=" + _lastAckPacketSeq + ", running fast retransmit of " + currentPacket);
#endif
                                RetryPacketSend();
                                Stats.DataPacketFastRetransmitCount++;
                                //_lastAckPacketCount = 0;
                            }
                        }
                        else
                        {
#if DEBUG
                            Logger.Debug("Discarding old ack " + packet);
#endif
                            Stats.OldAcksReceived++;
                        }
                        break;
                    }
                }
#if DEBUG
                Logger.Debug("Finished processing ack " + packet.Sequence + ", expecting next ack to be " +
                             (_sendWindow[_oldestUnackedPacket] == null
                                  ? "none"
                                  : _sendWindow[_oldestUnackedPacket].Sequence.ToString()));
#endif
                Monitor.PulseAll(_ackWaitingLock);
            }
        }

        private void AdjustRetryInterval(long rtt)
        {
            // Now we set the retry interval
            _srtt = (ushort)((_rtt_alpha * _srtt) + (_rtt_beta * rtt));
            _deviation = (_rtt_alpha * _deviation) + (_rtt_beta * Math.Abs(_srtt - rtt));
            _retryInterval = (int)(_srtt + (4 * _deviation));
        }

        private void RetryPacketSend()
        {
            ITcpPacket nextDataPacket;
            lock (_ackWaitingLock)
            {
                nextDataPacket = _sendWindow[_oldestUnackedPacket];
            }
            if (nextDataPacket != null)
            {
                if (Established)
                {
                    if (nextDataPacket.ResendCount > 15)
                    {
                        Logger.Warn("Resending packet " + nextDataPacket +
                                    " for the 15th time, it stops here, connection has died!");
                        Close();
                    }
                    else
                    {
                        nextDataPacket.ResendCount++;
                        Stats.DataPacketResendCount++;
                        // resend the packet and restart the timer
                        Transport.SendData(nextDataPacket);
                        Logger.Warn("Resent packet " + nextDataPacket.Sequence + " (resend=" +
                                     nextDataPacket.ResendCount + ") after timeout [" +
                                     _retryTimer.Interval + "ms], restarting timer to " + _retryInterval);
                        _retryTimer.Interval = _retryInterval;
                        _retryTimer.Start();
                    }
                }
                else
                {
                    Logger.Error("Cannot retry sending of " + nextDataPacket + ", connection has closed.");
                }
            }
        }

        public override void Close()
        {
            if (Established && !Closing && !Closed)
            {
                // block until DataSender completes
                Closing = true;
#if DEBUG
                Logger.Debug("Waiting for data sender to complete");
#endif
                _sendBuffer.Release();
                dataSenderThread.Join();
#if DEBUG
                Logger.Debug("Data sender has completed, continuing with shutting down.");
#endif
                // wait to empty our queue
                long startTime = Environment.TickCount;
                int elapsedTime = 0;
                int timeout = 30000;
                lock (_ackWaitingLock)
                {
                    // wait 10 seconds for it to close down nicely
#if DEBUG
                    Logger.Debug("Waiting for all outgoing data to be acked");
#endif
                    while (elapsedTime < timeout && _sendWindow[_oldestUnackedPacket] != null)
                    {
                        Monitor.Wait(_ackWaitingLock, timeout - elapsedTime);
                        elapsedTime = (int)(Environment.TickCount - startTime);
                    }
                    if (_sendWindow[_oldestUnackedPacket] != null)
                    {
                        Logger.Error("Closing connection while there are still unacked data packets, we have timed out this connection.");
                    }
                    else
                    {
#if DEBUG
                        Logger.Debug("All send data has been acked");
#endif
                    }
                }
                _Close();
#if DEBUG
                Logger.Debug("Sender Stats:\n" + Stats.GetSenderStats());
                Logger.Debug("Receiver Stats:\n" + Stats.GetReceiverStats());
#endif
                Closing = false;
                Closed = true;
            }
        }
    }
}
