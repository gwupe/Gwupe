using System;
using System.IO;
using System.Threading;
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
        private readonly ITcpDataPacket[] _sendWindow;
        private readonly ITcpDataPacket[] _recvWindow;
        private ITcpDataPacket _lastAckedPacket;
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
        private readonly System.Timers.Timer _retryTimer;
        private int _currentTimeout;
        private readonly Thread dataSenderThread;
        public TcpSlidingWindowStats Stats;
        //private ushort _lastAckPacketSeq = 0;
        private StandardAckPacket _lastAckPacket;
        private int _lastAckPacketCount = 0;
        private bool _disconnected;
        private event EventHandler DataSenderComplete;
        private readonly Object _processDataLock = new Object();
        private bool _isDisconnecting;
        private ushort _lastAckPacketSeq;

        public override ushort LastSeqSent
        {
            get { return (ushort)(_nextDataSeqOut - 1); }
        }

        public override ushort NextSeqToSend
        {
            get { return _nextDataSeqOut; }
        }

        public override byte ProtocolId { get { return 2; } }

        public TcpTransportLayerSlidingWindow(ITCPTransport transport, byte connectionId, byte remoteConnectionId, ushort firstLocalSequence, ushort firstRemoteSequence)
            : base(transport, connectionId, remoteConnectionId)
        {
            _sendWindow = new ITcpDataPacket[WindowSize];
            _recvWindow = new ITcpDataPacket[WindowSize];
            _sendBuffer = new CircularBuffer<byte>(BufferSize);
            Stats = new TcpSlidingWindowStats();
            // setup our start seq numbers and window numbering
            _sendWindowNextFree = _recvWindowsNextFree = _oldestUnackedPacket = 0;
            _nextDataSeqOut = firstLocalSequence;
            _nextDataSeqIn = firstRemoteSequence;
            _highestDataSeqIn = ushort.MaxValue;
            _retryTimer = new System.Timers.Timer(_retryInterval) { AutoReset = false };
            _retryTimer.Elapsed += delegate { RetryPacketSend(); };
            dataSenderThread = new Thread(DataSender) { IsBackground = true, Name = "dataSender[" + connectionId + "-" + remoteConnectionId + "]" };
            dataSenderThread.Start();
        }

        private void OnDataSenderComplete(EventArgs e)
        {
            EventHandler handler = DataSenderComplete;
            if (handler != null) handler(this, e);
        }

        // Received this data packet
        public override void ProcessDataPacket(ITcpDataPacket packet)
        {
            if (!Closed)
            {
                lock (_processDataLock)
                {
                    Stats.DataPacketsReceived++;
                    int compareSequences = BasicTcpPacket.CompareSequences(_nextDataSeqIn, packet.Sequence);
                    if (compareSequences == 0)
                    {
                        Stats.ExpectedDataPacketsReceived++;
                        if (BasicTcpPacket.CompareSequences(packet.Sequence, _highestDataSeqIn) > 0)
                            _highestDataSeqIn = packet.Sequence;
                        // This is exactly what we were expecting
                        // pop it in our list
                        ITcpDataPacket currentPacket = _recvWindow[_recvWindowsNextFree] = packet;
                        // Now flush our window until we get to a gap
                        while (_recvWindow[_recvWindowsNextFree] != null)
                        {
                            currentPacket = _recvWindow[_recvWindowsNextFree];
                            // Send Data Upstream (only it not closed)
                            if (!Socket.Closed && !Socket.Closing)
                            {
                                Socket.BufferClientData(currentPacket.Data);
                            }
#if DEBUG
                            else
                            {
                                Logger.Debug("Got a data packet, but not writing to socket, because its closed or closing");
                            }
#endif
                            // clear this space in our window
                            _recvWindow[_recvWindowsNextFree] = null;
                            // increment our pointer to next space
                            _recvWindowsNextFree = (byte)((_recvWindowsNextFree + 1) % WindowSize);
                            // increment our next next expected sequence number
                            _nextDataSeqIn++;
#if DEBUG
                            Logger.Debug("Processed sq=" + currentPacket.Sequence + " upstream, nextDataSeqIn=" +
                                         _nextDataSeqIn + ", recvWindowNextFree=" + _recvWindowsNextFree +
                                         ", highestDataSeqIn=" + _highestDataSeqIn);
#endif
                        }
                        // Send Ack (only when done with sending all the packets we can)
                        SendAck(currentPacket, currentPacket, true);
                        _lastAckedPacket = currentPacket;
                    }
                    else if (compareSequences > 0)
                    {
                        Stats.OldDataPacketsReceived++;
                        if (packet.ResendCount > 0) Stats.OldDataResendPacketsReceived++;
                        // This is an old packet, don't process (already did that), just send ack
#if DEBUG
                        Logger.Debug("Got old Data packet (resend=" + packet.ResendCount +
                                     "), sending ACK, data already processed.");
#endif
                        SendAck(packet, _lastAckedPacket, false);
                    }
                    else
                    {
                        Stats.FutureDataPacketsReceived++;
                        // This is an out of sequence packet from the future, 
                        // if it fits in our window, we need to save it for future use
                        int differenceInSequences = DifferenceInSequences(_nextDataSeqIn, packet.Sequence);
                        if (differenceInSequences < WindowSize)
                        {
                            // ok we can put it in
                            _recvWindow[(_recvWindowsNextFree + differenceInSequences) % WindowSize] = packet;
                            if (BasicTcpPacket.CompareSequences(packet.Sequence, _highestDataSeqIn) > 0)
                                _highestDataSeqIn = packet.Sequence;
#if DEBUG
                            Logger.Debug("Queued sq=" + packet.Sequence + " in window, nextDataSeqIn=" + _nextDataSeqIn +
                                        ", recvWindowNextFree=" + _recvWindowsNextFree + ", highestDataSeqIn=" +
                                        _highestDataSeqIn);
#endif
                            // Now we send an ack for the last in sequence packet we received
                            if (_lastAckedPacket != null)
                            {
                                SendAck(packet, _lastAckedPacket, false);
                                Stats.HoldingAcksSent++;
                            }
                        }
                        else
                        {
#if DEBUG
                            Logger.Debug("Got too far in the future sequence (expected " + _nextDataSeqIn + ") from packet " +
                                        packet + ", dropping");
#endif
                            // Now we send an ack for the last in sequence packet we received
                            if (_lastAckedPacket != null)
                            {
                                SendAck(_lastAckedPacket, _lastAckedPacket, false);
                                Stats.HoldingAcksSent++;
                            }
                        }
                    }
#if DEBUG
                    if (Stats.DataPacketsReceived % 1000 == 0)
                    {
                        Logger.Debug("Received Stats:\n" + Stats.GetReceiverStats());
                    }
#endif

                    Monitor.PulseAll(_processDataLock);
                }
            }
            else
            {
                Logger.Error("Failed to process data packet, the connection closed.");
            }
        }

        private void SendAck(ITcpDataPacket packet, ITcpDataPacket lastDataPacket, bool firstSend)
        {
            StandardAckPacket outPacket = new StandardAckPacket(packet.Sequence)
                {
                    ConnectionId = ConnectionId,
                    ResendCount = packet.ResendCount,
                    CurrentDataAck = lastDataPacket.Sequence
                };
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
                Logger.Error("Failed to send ack to peer for packet " + packet.Sequence + " : " + e.Message, e);
            }

        }

        public override void SendData(byte[] data, int length, int timeout)
        {
            if (!Closing && !Closed)
            {
                BlockIfNotEstablished(20000);
                _currentTimeout = timeout;
                _sendBuffer.Add(data, length, timeout);
                Stats.DataPacketBufferCount++;
#if DEBUG
                Logger.Debug("Added " + length + " bytes to the sendBuffer, " + _sendBuffer.Count + " in buffer");
                if (Stats.DataPacketSendCount % 1000 == 0)
                {
                    Logger.Debug("Sender Stats:\n" + Stats.GetSenderStats());
                }
#endif
            }
            else
            {
                throw new IOException("Cannot send data, connection has been closed");
            }
        }

        public void DataSender()
        {
            try
            {
                // if we still have stuff to send or we are still open
                while (
                    (_sendBuffer.Count > 0 || (!Closing && !Closed)) // we are closing and flushing, or not closing
                    && (!_isDisconnecting && !Disconnected)
                    )
                {
                    long waitStart = Environment.TickCount;
                    lock (_ackWaitingLock)
                    {
                        // wait until there is space in the window. ( we can't send stuff to far into the future )
                        while (!SpaceInWindow() && !Disconnected && !_isDisconnecting)
                        {
                            Stats.NoSpaceInWindow++;
#if DEBUG
                            Logger.Debug("Pausing send, no space left in window");
#endif
                            // timeout waiting for space in the window
                            if (!Monitor.Wait(_ackWaitingLock, _currentTimeout))
                            {
                                throw new TimeoutException("Timeout occured while waiting for space in the window to " + Transport.TransportManager.RemoteIp);
                            }
#if DEBUG
                            Logger.Debug("AckWaitLock release, probably space in window now");
#endif
                        }

                    }
                    byte[] payload = null;
                    if (!Disconnected && !_isDisconnecting)
                    {

                        // ok now there is space in the window, we will need to send some data and add it to the window.
                        payload = _sendBuffer.Get(TcpChunkSize);
                    }
                    if (!Disconnected && !_isDisconnecting)
                    {
                        if (payload != null && payload.Length > 0)
                        {
#if DEBUG
                            Logger.Debug("Got " + payload.Length + " bytes from the sendBuffer, " + _sendBuffer.Count +
                                         " left in buffer");
#endif
                            StandardTcpDataPacket packet = new StandardTcpDataPacket { Data = payload, ConnectionId = ConnectionId, Sequence = _nextDataSeqOut };
                            // make sure there is nothing in the next window space
                            if (_sendWindow[_sendWindowNextFree] != null)
                                throw new ConnectionException(
                                    "Cannot insert packet into window, there is something already there! [" +
                                    _sendWindow[_sendWindowNextFree].Sequence + "]");
                            // actually send the packet
                            Transport.SendData(packet);
                            // calculate some stats
                            Stats.AvgSendWaitTime = (Stats.AvgSendWaitTime * Stats.DataPacketSendCount +
                                                     (Environment.TickCount - waitStart)) /
                                                    (Stats.DataPacketSendCount + 1);
                            Stats.DataPacketSendCount++;
                            // timestamp the packet so we know when it was sent, this will help us when it comes to retrying packets
                            packet.Timestamp = Environment.TickCount;
                            // put it in the next free space in the window and update the free space, expected next data seq etc.
                            _sendWindow[_sendWindowNextFree] = packet;
                            _sendWindowNextFree = (byte)((_sendWindowNextFree + 1) % WindowSize);
                            _nextDataSeqOut++;
                            lock (_retryTimer)
                            {
                                // if we are not currently pending for a retry of an earlier packet, start the timer now.
                                if (!_retryTimer.Enabled)
                                {
                                    _retryTimer.Interval = _retryInterval;
                                    _retryTimer.Start();
#if DEBUG
                                    Logger.Debug("Setup retry time for " + packet.Sequence + " to " + _retryInterval +
                                                 "ms");
#endif
                                }
                            }
#if DEBUG
                            Logger.Debug("Sent packet " + packet.Sequence + ", nextDataSeqOut=" + _nextDataSeqOut +
                                         ", sendWindowNextFree=" + _sendWindowNextFree + ", oldestUnackedPacket=" +
                                         _oldestUnackedPacket);
#endif
                        }
                    }
                    else
                    {
                        Logger.Debug("Not sending packet, remote side is disconnecting or disconnected.");
                    }
                }
                Logger.Debug("Data sender has completed, buffer=" + _sendBuffer.Count + ", Closing=" + Closing + ", Closed=" + Closed + ", Disconnecting=" + _isDisconnecting + ", Disconnected=" + Disconnected);
            }
            catch (Exception e)
            {
                Logger.Error("DataSender caught Exception while running buffer : " + e.Message, e);
                Close();
            }
            OnDataSenderComplete(EventArgs.Empty);
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
                // update the rtt based on this packet if its a fresh ack and there is something in the window
                if (packet.ResendCount == 0 && _sendWindow[_oldestUnackedPacket] != null)
                {
                    // Is this packets seq ahead of the ones we are expecting
                    int seqDiff = BasicTcpPacket.CompareSequences(_sendWindow[_oldestUnackedPacket].Sequence,
                                                                  packet.Sequence);
                    if (seqDiff <= 0)
                    {
                        int distance = DifferenceInSequences(_sendWindow[_oldestUnackedPacket].Sequence,
                                                             packet.Sequence);
                        if (distance < WindowSize)
                        {
                            var matchingDataPacket = _sendWindow[(_oldestUnackedPacket + distance) % WindowSize];
                            if (matchingDataPacket != null && matchingDataPacket.Sequence == packet.Sequence)
                            {
                                long rtt = Environment.TickCount - matchingDataPacket.Timestamp;
                                AdjustRetryInterval(rtt);
#if DEBUG
                                Logger.Debug("RTT of " + matchingDataPacket + " is " + rtt + ", srtt=" + _srtt +
                                             ", deviation=" + _deviation + ", retryInterval=" + _retryInterval);
#endif
                            }
                            else
                            {
                                Logger.Error("Got an ack " + packet + " but can't find matching data packet, oldest unacked is " 
                                    + _sendWindow[_oldestUnackedPacket] + ", nextSeqOut=" + _nextDataSeqOut + ", nextSeqIn=" + _nextDataSeqIn + ", lastAckPaket=" + _lastAckPacket);
                            }
                        }
                        else
                        {
                            Logger.Error("Got an ack for a message too far in the future [distance=" + distance + "]");
                        }
                    }
                    else
                    {
#if DEBUG
                        Logger.Debug("Got an ack from the past, cannot use it to calculate RTT");
#endif
                    }
                }

                // loop through expected acks clearing them until we are in sync with incoming ack packet
                while (_sendWindow[_oldestUnackedPacket] != null)
                {
                    // current data packet in the window
                    var currentDataPacket = _sendWindow[_oldestUnackedPacket];
                    int seqDiff = BasicTcpPacket.CompareSequences(currentDataPacket.Sequence,
                                                                  packet.CurrentDataAck);
                    // If this incoming packet's currentDataAck is the one we are expecting (==) or one ahead of our current 
                    // expected ack < (indicating receiver has all those up until this one)
                    if (seqDiff <= 0)
                    {
                        // null the packet (its acked)
                        _sendWindow[_oldestUnackedPacket] = null;
                        // oldestUnackedPaket is now the next one
                        _oldestUnackedPacket = (byte)((_oldestUnackedPacket + 1) % WindowSize);
#if DEBUG
                        Logger.Debug("Cleared old packet " + currentDataPacket.Sequence + ", oldestUnackedPacket=" +
                                     _oldestUnackedPacket);
#endif

                        // save the last ack we received (and the count)
                        _lastAckPacketSeq = currentDataPacket.Sequence;
                        // and we have received it once
                        _lastAckPacketCount = 1;
                        // if the current pack is the exact one we received an ack for
                        // then lets reset the timer and leave the loop
                        if (seqDiff == 0)
                        {
                            lock (_retryTimer)
                            {
                                // reset the time against the next unacked packet
                                long timeLeft = _retryInterval - (Environment.TickCount - currentDataPacket.Timestamp);
                                if (timeLeft <= 0) timeLeft = 1;
                                if (_retryTimer.Enabled)
                                {
                                    _retryTimer.Stop();
                                }
                                _retryTimer.Interval = timeLeft;
#if DEBUG
                                Logger.Debug("Setup retransmit timeout for " + currentDataPacket.Sequence + " to " +
                                             timeLeft + "ms.");
#endif
                                _retryTimer.Start();
                            }
                            break;
                        }
                    }
                    else
                    {
                        if (packet.CurrentDataAck == _lastAckPacketSeq)
                        {
                            // this is an old ack, if we get 3, we retrasmit the next one
                            _lastAckPacketCount++;
#if DEBUG
                            Logger.Debug("Discarding the repeated ack " + packet + "[" + _lastAckPacketCount + " times], checking for retransmit");
#endif
                            // we only resend via fast transmit if it is a count of 3 and it hasn't been resent before
                            if (_lastAckPacketCount == 3)
                            {
                                RunFastRetransmit(currentDataPacket);
                            }
                        }
                        else
                        {
                            // old ack too far back to care
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

        private void RunFastRetransmit(ITcpDataPacket currentDataPacket)
        {
            lock (_retryTimer)
            {
                if (_retryTimer.Enabled)
                {
                    _retryTimer.Stop();
                }
#if DEBUG
                Logger.Debug("Received 3 acks of seq=" + _lastAckPacketSeq + ", running fast retransmit of " + currentDataPacket);
#endif
                // fast retransmit
                RetryPacketSend();
                Stats.DataPacketFastRetransmitCount++;
                //_lastAckPacketCount = 0;
            }
        }


        private void AdjustRetryInterval(long rtt)
        {
            // Now we set the retry interval
            _srtt = (ushort)((_rtt_alpha * _srtt) + (_rtt_beta * rtt));
            _deviation = (_rtt_alpha * _deviation) + (_rtt_beta * Math.Abs(_srtt - rtt));
            _retryInterval = (int)(_srtt + (4 * _deviation));
            if (_retryInterval < 1)
            {
#if DEBUG
                Logger.Debug("_retryInterval was too low, adjusting it to be 1");
#endif
                _retryInterval = 1;
            }
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
                if (!Closed)
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
                        // Implement a retry backoff here.
                        //_retryInterval = _retryInterval * 2;
#if DEBUG
                        Logger.Warn("Resent packet " + nextDataPacket.Sequence + " (resend=" +
                                     nextDataPacket.ResendCount + ") after timeout [" +
                                     _retryTimer.Interval + "ms], restarting timer to " + _retryInterval);
#endif
                        _retryTimer.Interval = _retryInterval * 2;
                        _retryTimer.Start();
                    }
                }
                else
                {
                    Logger.Error("Cannot retry sending of " + nextDataPacket + ", connection has closed.");
                }
            }
        }

        public override void ProcessDisconnect(StandardDisconnectPacket packet)
        {
            if (!_isDisconnecting)
            {
                _isDisconnecting = true;
                // Turn off our data sender, remote host won't process the data anyway
                // release a potentially blocked data sender
                ReleaseDataSender();
                // clear the data retry timer
                if (_retryTimer.Enabled)
                {
                    _retryTimer.Stop();
                }

                if (Stats.DataPacketsReceived > 0 && packet.Sequence == (_nextDataSeqIn - 1))
                {
                    // Wait for all data to arrive (disconnect notified us of the last seq)
                    long startTime = Environment.TickCount;
                    int elapsedTime = 0;
                    const int timeout = 60000;
                    lock (_processDataLock)
                    {
#if DEBUG
                        Logger.Debug("Waiting for all data which was sent to arrive, up until " + packet.Sequence);
#endif
                        while (elapsedTime < timeout &&
                               (_lastAckedPacket == null || _lastAckedPacket.Sequence < packet.Sequence))
                        {
                            Monitor.Wait(_processDataLock, timeout - elapsedTime);
                            elapsedTime = (int)(Environment.TickCount - startTime);
                        }
                    }
                    if (_lastAckedPacket == null || _lastAckedPacket.Sequence < packet.Sequence)
                    {
                        Logger.Error(
                            "Failed to receive all the sent data packets during disconnect, timeout while waiting, last acked packet is " +
                            _lastAckedPacket + ", we wanted " + packet.Sequence);
                    }
                    else
                    {
                        Logger.Debug("We have successfully received all the data that was sent : expected " +
                                     packet.Sequence + ", got " + _lastAckedPacket.Sequence);
                    }
                }
                else
                {
                    Logger.Debug("Don't have to wait for all data to arrive, because none has been sent.");
                }
                // Mark it as disconnected
                _disconnected = true;

                // Now close upstream
                if (Established && !Closing && !Closed)
                {
                    Logger.Debug("Closing connection after disconnect " + ConnectionId + "-" + RemoteConnectionId);
                    Closing = true;
                    var closer = new Thread(CompleteClose) { IsBackground = true, Name = "closer[" + ConnectionId + "-" + RemoteConnectionId + "]" };
                    closer.Start();
                }
            }
        }

        private void ReleaseDataSender()
        {
            lock (_ackWaitingLock)
            {
                Monitor.PulseAll(_ackWaitingLock);
            }
            _sendBuffer.Release();
        }

        public override bool Disconnected
        {
            get { return _disconnected; }
        }

        public override void Close()
        {
            if (Established && !Closing && !Closed)
            {
                Logger.Debug("Closing connection " + ConnectionId + "-" + RemoteConnectionId);
                Closing = true;
                // if the data sender is alive, then make sure close completes after, otherwise fire of a background job to close it now.
                if (dataSenderThread != null && dataSenderThread.IsAlive)
                {
                    DataSenderComplete += delegate
                        {
                            Logger.Debug("Data Sender has completed, closing remaining connections");
                            CompleteClose();
                        };
                    ReleaseDataSender();
                }
                else
                {
                    var closer = new Thread(CompleteClose) { IsBackground = true, Name = "closer[" + ConnectionId + "-" + RemoteConnectionId + "]" };
                    closer.Start();
                }

            }
        }

        private void CompleteClose()
        {
            Established = false;
            // close the socket (this will return immediately if the socket is already 
            // closed doing nothing further, i.e. if the close has come from upstream)
            Socket.Close();
            // wait to empty our window of unacked data
            WaitOnPendingAcks();
            // Log
            Logger.Info("Sender Stats:\n" + Stats.GetSenderStats());
            Logger.Info("Receiver Stats:\n" + Stats.GetReceiverStats());
            // close the connection maintained by the transport
            OnConnectionClose(EventArgs.Empty);
            // Close
            Closing = false;
            Closed = true;
        }

        private void WaitOnPendingAcks()
        {
            // only wait if it isn't disconnected
            if (!_isDisconnecting && !Disconnected)
            {
                long startTime = Environment.TickCount;
                int elapsedTime = 0;
                int timeout = 60000;
                lock (_ackWaitingLock)
                {
                    // wait 60 seconds for it to close down nicely
                    Logger.Debug("Waiting for all outgoing data to be acked");
                    while (elapsedTime < timeout && _sendWindow[_oldestUnackedPacket] != null)
                    {
                        Monitor.Wait(_ackWaitingLock, timeout - elapsedTime);
                        elapsedTime = (int)(Environment.TickCount - startTime);
                    }
                    if (_sendWindow[_oldestUnackedPacket] != null)
                    {
                        Logger.Error(
                            "Closing connection while there are still unacked data packets, we have timed out this connection.");
                    }
                    else
                    {
                        Logger.Debug("All send data has been acked");
                    }
                }
            }
        }
    }
}
