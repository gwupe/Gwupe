using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using BlitsMe.Communication.P2P.RUDP.Packet.Tunnel;
using log4net;

namespace BlitsMe.Communication.P2P.RUDP.Utils
{
    public class Pinger
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Pinger));
        private readonly AutoResetEvent _pingEvent = new AutoResetEvent(false);
        private byte _pingSeq;
        public String Id { get; private set; }

        public Pinger(String id)
        {
            _pingSeq = 0;
            this.Id = id;
        }

        public int Ping(IPEndPoint ipEndPoint, int timeout, UdpClient udpClient)
        {
            StandardPingTunnelPacket ping = new StandardPingTunnelPacket() { data = new[] { _pingSeq++ } };
            long startTime = DateTime.Now.Ticks;
            _pingEvent.Reset();
            byte[] bytes = ping.getBytes();
            udpClient.Send(bytes, bytes.Length, ipEndPoint);
            if (!_pingEvent.WaitOne(timeout))
            {
                throw new TimeoutException("Timeout occured while attempting to ping [seq=" + ping.data[0] + "] " + ipEndPoint);
            }
            long stopTime = DateTime.Now.Ticks;
            int pingTimeMillis = (int)((stopTime - startTime) / 10000);
            Logger.Debug("Successfully UDP pinged [seq=" + ping.data[0] + "] tunnel " + Id + " in " + pingTimeMillis + "ms");
            return pingTimeMillis;

        }

        public void ProcessPing(StandardPingTunnelPacket ping, UdpClient udpClient)
        {
            StandardPongTunnelPacket pong = new StandardPongTunnelPacket() { data = ping.data };
            byte[] bytes = pong.getBytes();
            udpClient.Send(bytes, bytes.Length, ping.ip);
            //Logger.Debug("Sent a UDP pong [seq=" + ping.data[0] + "] to " + ping.ip);

        }

        public void ProcessPong(StandardPongTunnelPacket packet)
        {
            _pingEvent.Set();
        }

    }
}

