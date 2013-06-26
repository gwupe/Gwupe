using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using BlitsMe.Communication.P2P.RUDP.Packet.API;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;
using BlitsMe.Communication.P2P.RUDP.Tunnel.Transport;
using log4net;

namespace BlitsMe.TransportEmulator
{
    class ProxyTransportManager : ITransportManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ProxyTransportManager));
        private readonly TransportForm _transportForm;
        private readonly bool _client;
        private bool _isActive;
        public ITCPTransport TCPTransport { get; private set; }
        public UDPTransport UDPTransport { get; private set; }

        public bool IsActive
        {
            get { return _isActive; }
        }

        public event EventHandler Active;

        public void OnActive(EventArgs e)
        {
            EventHandler handler = Active;
            if (handler != null) handler(this, e);
        }

        public event EventHandler Inactive;

        public void OnInactive(EventArgs e)
        {
            EventHandler handler = Inactive;
            if (handler != null) handler(this, e);
        }

        public ProxyTransportManager proxy;
        public Stopwatch stopwatch;
        private Random rand;
        public readonly Queue PhysicalLayer;
        private readonly Thread _physicalTransporterThread;
        public readonly object PhysicalLayerLock = new Object();
        private int _bytesThisPeriod = 0;
        private int _periodStart;
        private const int PeriodLength = 1000;

        public ProxyTransportManager(TransportForm transportForm, bool client)
        {
            _isActive = true;
            OnActive(EventArgs.Empty);
            stopwatch = new Stopwatch();
            _transportForm = transportForm;
            _client = client;
            rand = new Random(); ;
            TCPTransport = new TCPTransport(this);
            PhysicalLayer = Queue.Synchronized(new Queue());
            _physicalTransporterThread = new Thread(PhysicalLayerPump) { IsBackground = true, Name = _client ? "ClientReceiveThread" : "ServerReceiveThread" };
            _physicalTransporterThread.Start();
        }

        public void SendData(IPacket packet)
        {
            //stopwatch.Start();
            SendToDestination(packet);
            //stopwatch.Stop();
            //stopwatch.Reset();
        }

        private void SendToDestination(IPacket packet)
        {
            int periodLeft = PeriodLength - (Environment.TickCount - _periodStart);
            if (periodLeft <= 0)
            {
                Logger.Info("Resetting period, no more time left in period");
                ResetPeriod();
                periodLeft = PeriodLength;
            }
            if (_bytesThisPeriod > _transportForm.Bandwidth * 1024)
            {
                // wait out the period
                Logger.Info("Blocking, already sent " + _bytesThisPeriod + " this period, will wait " + periodLeft + "ms");
                Thread.Sleep(periodLeft);
                ResetPeriod();
                periodLeft = PeriodLength;
            }
            _bytesThisPeriod += packet.GetBytes().Length;
            // drop the packet here
            if (rand.Next(100) < _transportForm.PacketLoss)
            {
                Logger.Warn("Ooops dropped a packet : " + packet);
                if (_client)
                    _transportForm.ClientPacketLoss++;
                else
                    _transportForm.ServerPacketLoss++;
            }
            else
            {
                // dump it onto the physical layer of the other proxy
                proxy.PhysicalLayer.Enqueue(new PhysicalPacketHolder(packet));
                Logger.Info("Sent packet through physical layer, " + _bytesThisPeriod + " so far this period, " + periodLeft + "ms left.");
                lock (proxy.PhysicalLayerLock)
                    Monitor.Pulse(proxy.PhysicalLayerLock);
            }


        }

        private void ResetPeriod()
        {
            // reset the period
            _periodStart = Environment.TickCount;
            _bytesThisPeriod = 0;
        }


        public IPAddress RemoteIp { get { return IPAddress.Loopback; } }

        public void AddTunnel(IUDPTunnel tunnel, int priority)
        {
        }

        public void Close()
        {
        }

        public void SetProxy(ProxyTransportManager transportManager)
        {
            proxy = transportManager;
        }

        private void PhysicalLayerPump()
        {
            while (true)
            {
                lock (PhysicalLayerLock)
                {
                    while (PhysicalLayer.Count == 0)
                        Monitor.Wait(PhysicalLayerLock);
                }
                long currentLatencyValue = _transportForm.Latency;
                while (PhysicalLayer.Count > 0)
                {
                    var packetHolder = (PhysicalPacketHolder)PhysicalLayer.Dequeue();
                    long latencySoFar = Environment.TickCount - packetHolder._sendTime;
                    // latency is done here
                    if (latencySoFar < currentLatencyValue)
                    {
                        //Logger.Info("Implementing remainder latency of " + (currentLatencyValue - latencySoFar) + "ms");
                        Thread.Sleep(TimeSpan.FromMilliseconds(currentLatencyValue - latencySoFar));
                    }
                    // Now pass this onto my Transport
                    Logger.Info("Received packet [size=" + packetHolder._packet.Data.Length + "] from physical layer, " + PhysicalLayer.Count + " still on the wire.");
                    TCPTransport.ProcessPacket(packetHolder._packet.GetBytes());
                    var actualLatency = Environment.TickCount - packetHolder._sendTime;
                    if (_client)
                        _transportForm.ClientLatency = actualLatency;
                    else
                        _transportForm.ServerLatency = actualLatency;
                }

            }
        }
    }

    internal class PhysicalPacketHolder
    {
        internal IPacket _packet;
        internal long _sendTime;

        public PhysicalPacketHolder(IPacket packet)
        {
            this._sendTime = Environment.TickCount;
            this._packet = packet;
        }


    }
}
