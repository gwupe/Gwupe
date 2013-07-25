using System;
using System.Collections.Generic;
using System.Linq;
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
        private UdpClient _udpClient;

        public PeerInfo Wave(IPEndPoint facilitatorIp, int timeout, UdpClient udpClient)
        {
            long waitTime = timeout * 10000;
            _udpClient = udpClient;
            Logger.Debug("Waving " + facilitatorIp + " for " + timeout + "ms");
            //var localEndPoints = GetLocalEndPoints(udpClient);
            //IPEndPoint myEndPoint = localEndPoints.Count > 0 ? localEndPoints[0] : new IPEndPoint(IPAddress.Any, ((IPEndPoint)udpClient.Client.LocalEndPoint).Port);
            var packet = new StandardWaveTunnelRqPacket { internalEndPoint = new IPEndPoint(IPAddress.Any, ((IPEndPoint)udpClient.Client.LocalEndPoint).Port) };
            long startTime = DateTime.Now.Ticks;
            _waveEvent.Reset();
            do
            {
                byte[] sendBytes = packet.getBytes();
                udpClient.Send(sendBytes, sendBytes.Length, facilitatorIp);
                if (DateTime.Now.Ticks - startTime > waitTime)
                {
                    Logger.Error("Wave timeout : " + (DateTime.Now.Ticks - startTime));
                    //throw new TimeoutException("Timeout occured while waving");
                    break;
                }
                Logger.Debug("Waiting for wave response from " + facilitatorIp);
            } while (!_waveEvent.WaitOne(2000));
            if(_waveResult == null)
            {
                _waveResult = new PeerInfo();
            }
            _waveResult.InternalEndPoints = GetLocalEndPoints(_udpClient);
            return _waveResult;
        }

        public void ProcessWaveRs(StandardWaveTunnelRsPacket packet)
        {
            Logger.Debug("Processing Wave Response from " + packet.ip);
            _waveResult = new PeerInfo() { ExternalEndPoint = packet.externalEndPoint };
            _waveEvent.Set();
        }

        private IPEndPoint GetLocalEndPoint(IPEndPoint ip, UdpClient udpClient)
        {
            var localEndPoint = new IPEndPoint(GuessLocalIp(), ((IPEndPoint)udpClient.Client.LocalEndPoint).Port);
            Logger.Debug("Got local endpoint as " + localEndPoint.ToString());
            return localEndPoint;
        }

        private List<IPEndPoint> GetLocalEndPoints(UdpClient udpClient)
        {
            return GetLocalIps().Select(ipAddress => new IPEndPoint(ipAddress, ((IPEndPoint)udpClient.Client.LocalEndPoint).Port)).ToList();
        }

        private IPAddress GetDefaultIp(IPEndPoint ip)
        {
            var ping = new Ping();
            var options = new PingOptions { Ttl = 1 };
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

        private List<IPAddress> GetLocalIps()
        {
            var ips = new List<IPAddress>();
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface nic in adapters)
            {
                if (nic.OperationalStatus == OperationalStatus.Up &&
                    (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet3Megabit ||
                    nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                    nic.NetworkInterfaceType == NetworkInterfaceType.FastEthernetT ||
                    nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                    nic.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet ||
                    nic.NetworkInterfaceType == NetworkInterfaceType.FastEthernetFx))
                {
                    foreach (UnicastIPAddressInformation ipInfo in nic.GetIPProperties().UnicastAddresses)
                    {
                        byte[] address = ipInfo.Address.GetAddressBytes();
                        if (address.Length == 4)
                        {
                            if ((address[0] == 172 && address[1] >= 16 && address[1] <= 31)
                                || (address[0] == 10)
                                || (address[0] == 192 && address[1] == 168))
                            {
                                Logger.Debug("Got local interface " + ipInfo.Address);
                                ips.Add(ipInfo.Address);
                            }
                        }
                    }
                }
            }
            return ips;
        }

        private IPAddress GuessLocalIp()
        {
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface nic in adapters)
            {
                if (nic.OperationalStatus == OperationalStatus.Up &&
                    (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet3Megabit ||
                    nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                    nic.NetworkInterfaceType == NetworkInterfaceType.FastEthernetT ||
                    nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                    nic.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet ||
                    nic.NetworkInterfaceType == NetworkInterfaceType.FastEthernetFx))
                {
                    foreach (UnicastIPAddressInformation ipInfo in nic.GetIPProperties().UnicastAddresses)
                    {
                        byte[] address = ipInfo.Address.GetAddressBytes();
                        if (address.Length == 4)
                        {
                            if ((address[0] == 172 && address[1] >= 16 && address[1] <= 31)
                                || (address[0] == 10)
                                || (address[0] == 192 && address[1] == 168))
                                return ipInfo.Address;
                        }
                    }
                }
            }
            return IPAddress.Any;
        }
    }
}
