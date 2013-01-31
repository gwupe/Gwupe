using System;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;
using BlitsMe.Communication.P2P.RUDP.Packet.Tunnel;
using log4net;

namespace BlitsMe.Communication.P2P.RUDP.Utils
{
    internal class Waver
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Waver));
        private readonly AutoResetEvent _waveEvent = new AutoResetEvent(false);
        private PeerInfo _waveResult;

        public Waver()
        {
        }

        public PeerInfo Wave(System.Net.IPEndPoint facilitatorIp, int timeout, UdpClient udpClient)
        {
            long waitTime = timeout * 10000;
#if(DEBUG)
            Logger.Debug("Waving " + facilitatorIp + " for " + timeout + "ms");
#endif
            IPEndPoint myEndPoint = GetLocalEndPoint(facilitatorIp, udpClient);
            var packet = new StandardWaveTunnelRqPacket();
            packet.internalEndPoint = myEndPoint;
            long startTime = DateTime.Now.Ticks;
            _waveEvent.Reset();
            do
            {
                byte[] sendBytes = packet.getBytes();
                udpClient.Send(sendBytes, sendBytes.Length, facilitatorIp);
                if (DateTime.Now.Ticks - startTime > waitTime)
                {
#if(DEBUG)
                    Logger.Debug("Wave timeout : " + (DateTime.Now.Ticks - startTime));
#endif
                    throw new TimeoutException("Timeout occured while waving");
                }
#if(DEBUG)
                Logger.Debug("Waiting for wave response from " + facilitatorIp);
#endif
            } while (!_waveEvent.WaitOne(2000));

            return _waveResult;
        }

        public void ProcessWaveRs(StandardWaveTunnelRsPacket packet)
        {
#if(DEBUG)
            Logger.Debug("Processing Wave Response from " + packet.ip);
#endif
            _waveResult = new PeerInfo(packet.internalEndPoint, packet.externalEndPoint);
            _waveEvent.Set();
        }

        private IPEndPoint GetLocalEndPoint(IPEndPoint ip, UdpClient udpClient)
        {
            var localEndPoint = new IPEndPoint(getDefaultIp(ip), ((IPEndPoint)udpClient.Client.LocalEndPoint).Port);
#if DEBUG
            Logger.Debug("Got local endpoint as " + localEndPoint.ToString());
#endif
            return localEndPoint;
        }

        private IPAddress getDefaultIp(IPEndPoint ip)
        {
            var ping = new System.Net.NetworkInformation.Ping();
            var options = new PingOptions {Ttl = 1};
            PingReply reply = ping.Send(ip.Address.ToString(), 5000, new byte[32], options);
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface nic in adapters)
            {
                foreach (GatewayIPAddressInformation gw in nic.GetIPProperties().GatewayAddresses)
                {
                    if (gw.Address.Equals(reply.Address))
                    {
                        foreach (UnicastIPAddressInformation ipInfo in nic.GetIPProperties().UnicastAddresses)
                        {
                            if (!ipInfo.Address.IsIPv6LinkLocal)
                            {
                                return ipInfo.Address;
                            }
                        }
                    }
                }
            }
            return IPAddress.Any;
        }
    }
}
