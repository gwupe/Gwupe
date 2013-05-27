using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
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
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TCPConnectionHelper));
        private readonly AutoResetEvent _connectEvent = new AutoResetEvent(false);
        private StandardNamedConnectRsPacket _connectResult;

        public byte ConnectNamed(byte connectionId, String namedEndPoint, int timeout, TCPTransport transport)
        {
            long waitTime = timeout * 1000;
#if(DEBUG)
            Logger.Debug("Connecting to " + namedEndPoint + ", timeout is " + timeout + "ms");
#endif
            StandardNamedConnectRqPacket packet = new StandardNamedConnectRqPacket(connectionId)
                {
                    connectionName = namedEndPoint
                };
            long startTime = DateTime.Now.Ticks;
            _connectEvent.Reset();
            do
            {
#if(DEBUG)
                Logger.Debug("Sending packet : " + packet.ToString());
#endif
                transport.SendData(packet);
                var timeSpan = new TimeSpan(DateTime.Now.Ticks - startTime);
                if (timeSpan.TotalMilliseconds > waitTime)
                {
#if(DEBUG)
                    Logger.Debug("Connect timeout : " + timeSpan.TotalMilliseconds + "ms");
#endif
                    throw new TimeoutException("Timeout occured while connecting to " + namedEndPoint);
                }
#if(DEBUG)
                Logger.Debug("Waiting for connect response from " + namedEndPoint);
#endif
            } while (!_connectEvent.WaitOne(1000));

            if (_connectResult.success)
            {
                return _connectResult.remoteConnectionId;
            }
            else
            {
                throw new ConnectionException("Failed to connect to " + namedEndPoint + ", service is unavailable");
            }
        }

        public void ProcessConnectRs(StandardNamedConnectRsPacket packet)
        {
#if(DEBUG)
            Logger.Debug("Processing Connect Response " + packet);
#endif
            _connectResult = packet;
            _connectEvent.Set();
        }

        public void CloseConnection(byte connectionId, TCPTransport transport)
        {
#if(DEBUG)
            Logger.Debug("Disconnecting tcp session " + connectionId);
#endif
            try
            {
                transport.SendData(new StandardDisconnectPacket(connectionId));
            }
            catch (Exception e)
            {
                Logger.Error("Failed to send the close packet, closing connection anyway. : " + e.Message,e);
            }
        }

        public void AckConnectionResponse(byte connectionId, TCPTransport transport) {
#if(DEBUG)
            Logger.Debug("Acking tcp connection " + connectionId);
#endif
            transport.SendData(new StandardConnectRsAckPacket(connectionId));
        }
    }
}
