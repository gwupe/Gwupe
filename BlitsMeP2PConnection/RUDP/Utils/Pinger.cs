using System;
using System.Collections.Generic;
using System.Linq;
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

        public Pinger()
        {
            _pingSeq = 0;
        }

        public int Ping(PeerInfo peer, int timeout, UdpClient udpClient)
        {
            StandardPingTunnelPacket ping = new StandardPingTunnelPacket() { data = new[] { _pingSeq++ } };
            long startTime = DateTime.Now.Ticks;
            _pingEvent.Reset();
            byte[] bytes = ping.getBytes();
            udpClient.Send(bytes, bytes.Length, peer.externalEndPoint);
            if (!_pingEvent.WaitOne(timeout))
            {
                throw new TimeoutException("Timeout occured while attempting to ping [seq=" + ping.data[0] + "] " + peer.externalEndPoint);
            }
            long stopTime = DateTime.Now.Ticks;
            int pingTimeMillis = (int)((stopTime - startTime) / 10000);
#if(DEBUG)
            Logger.Debug("Successfully UDP pinged [seq=" + ping.data[0] + "] " + peer.externalEndPoint + " in " + pingTimeMillis + "ms");
#endif
            return pingTimeMillis;

        }

        public void ProcessPing(StandardPingTunnelPacket ping, UdpClient udpClient)
        {
            StandardPongTunnelPacket pong = new StandardPongTunnelPacket() { data = ping.data };
            byte[] bytes = pong.getBytes();
            udpClient.Send(bytes, bytes.Length, ping.ip);
#if(DEBUG)
            Logger.Debug("Sent a UDP pong [seq=" + ping.data[0] + "] to " + ping.ip);
#endif

        }

        public void ProcessPong(StandardPongTunnelPacket packet)
        {
            _pingEvent.Set();
        }

    }
}

