using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlitsMe.Communication.P2P.Exceptions;
using BlitsMe.Communication.P2P.RUDP.Connector.API;
using BlitsMe.Communication.P2P.RUDP.Socket.API;
using System.Net;
using System.Net.Sockets;
using log4net;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;

namespace BlitsMe.Communication.P2P.RUDP.Connector
{
    public class ProxyTcpTransportListener : TcpTransportListener
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ProxyTcpTransportListener));
        public IPEndPoint ProxyEndPoint { get; private set; }

        public ProxyTcpTransportListener(String name, IPEndPoint endPoint, ITransportManager transportManager) : base(name, transportManager)
        {
            ProxyEndPoint = endPoint;
            //ProcessConnect = ProcessProxyConnect;
        }


        #region Connection Processing

        protected override TcpTransportConnection ProcessConnect(ITcpOverUdptSocket socket)
        {
            try
            {
                // Attempt to connect end point
                TcpClient client = new TcpClient();
                client.Connect(ProxyEndPoint);
                // Setup the proxying threads
                ProxyTcpConnection proxyTcpConnection = new ProxyTcpConnection(client, socket);
                return proxyTcpConnection;
            }
            catch (Exception e)
            {
                Logger.Error(
                    "Failed to connect to proxy endpoint [" + ProxyEndPoint + "], cannot proxy : " + e.Message, e);
                return null;
            }
        }

        #endregion

    }
}
