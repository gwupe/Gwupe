using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using Gwupe.Communication.P2P.RUDP.Packet;
using Gwupe.Communication.P2P.RUDP.Packet.Tunnel;
using log4net;

namespace Gwupe.Communication.P2P.P2P.Tunnel
{
    public class Waver
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Waver));
        private readonly AutoResetEvent _waveEvent = new AutoResetEvent(false);
        private PeerInfo _waveResult;
        private UdpClient _udpClient;
        private Thread _waveListenerThread;

        public PeerInfo Wave(IPEndPoint facilitatorIp, int timeout, UdpClient udpClient)
        {
            long waitTime = timeout * 10000;
            _udpClient = udpClient;
            Logger.Debug("Waving at " + facilitatorIp + " from local port " + ((IPEndPoint)udpClient.Client.LocalEndPoint).Port + " for " + timeout + "ms");
            var packet = new StandardWaveTunnelRqPacket { internalEndPoint = new IPEndPoint(IPAddress.Any, ((IPEndPoint)udpClient.Client.LocalEndPoint).Port) };
            long startTime = DateTime.Now.Ticks;
            _waveEvent.Reset();
            // Setup a listener for a wave response
            InitReceiverWaveRs();
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
                Logger.Debug("Waiting for wave response from " + facilitatorIp + " to local port " + ((IPEndPoint)udpClient.Client.LocalEndPoint).Port);
            } while (!_waveEvent.WaitOne(2000));
            if(_waveResult == null)
            {
                _waveResult = new PeerInfo();
            }
            _waveResult.InternalEndPoints = GetLocalEndPoints(_udpClient);
            return _waveResult;
        }

        private void InitReceiverWaveRs()
        {
            _waveListenerThread = new Thread(ListenForPackets) { IsBackground = true, Name = "_waveListenerThread" };
            _waveListenerThread.Start();
        }

        private void ListenForPackets()
        {
            while (true)
            {
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                Byte[] bytes = null;
                try
                {
                    bytes = _udpClient.Receive(ref RemoteIpEndPoint);
                    BasicTunnelPacket packet = StandardUdpPacketFactory.instance.getPacket(bytes, RemoteIpEndPoint);
                    if (packet.type == BasicTunnelPacket.PKT_TYPE_WAVE_RS)
                    {
#if(DEBUG)
                        Logger.Debug("Got a wave response from " + packet.ip + ", my address is " +
                                     ((StandardWaveTunnelRsPacket) packet).externalEndPoint + "/" +
                                     ((StandardWaveTunnelRsPacket) packet).internalEndPoint);
#endif
                        ProcessWaveRs((StandardWaveTunnelRsPacket) packet);
                        Logger.Debug("Shutting down wave listener");
                        break;
                    }
                    else
                    {
                        Logger.Error("Waiting for a wave response, but got unknown packet");
                        break;
                    }
                }
                catch (SocketException e)
                {
#if(DEBUG)
                    Logger.Debug("Caught a socket exception [" + e.ErrorCode + "] : " + e.Message);
#endif
                    if (e.ErrorCode == 10004) // Interrupted
                    {
#if(DEBUG)
                        Logger.Debug("Socket has been interrupted, shutting down");
#endif
                        _udpClient.Close();
                        break;
                    }
                    else if (e.ErrorCode == 10054)
                        // Got ICMP connection closed ( we need to ignore this, hole punching causes these during init )
                    {
#if(DEBUG)
                        Logger.Debug("Remote host stated ICMP port closed, ignoring");
#endif
                    }
                    else
                    {
                        Logger.Warn("Caught a socket exception [" + e.ErrorCode +
                                    "], this looks spurious, ignoring : " + e.Message);
                        Logger.Error("Caught a socket exception [" + e.ErrorCode + "], shutting down read thread : " +
                                     e.Message);
                        _udpClient.Close();
                        break;
                    }
                }
                catch (ThreadAbortException e)
                {
#if DEBUG
                    Logger.Debug("Thread is aborting, closing : " + e.Message);
#endif
                    _udpClient.Close();
                    break;
                }
                catch (Exception e)
                {
                    Logger.Error("Exception while reading from UDP socket, shutting down read thread : " + e.Message, e);
                    // Most likely the link has failed (this side) or the app is closing
                    // either way, close the thread for the moment
                    _udpClient.Close();
                    break;
                }
            }
        }

        public void ProcessWaveRs(StandardWaveTunnelRsPacket packet)
        {
            Logger.Debug("Processing Wave Response from " + packet.ip);
            _waveResult = new PeerInfo() { ExternalEndPoint = packet.externalEndPoint, FacilitatorRepeatedEndPoint = packet.ip };
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
