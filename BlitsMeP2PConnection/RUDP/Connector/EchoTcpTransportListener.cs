using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlitsMe.Communication.P2P.RUDP.Connector.API;
using BlitsMe.Communication.P2P.RUDP.Socket.API;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;
using log4net;

namespace BlitsMe.Communication.P2P.RUDP.Connector
{
    public class EchoTcpTransportListener : TcpTransportListener
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (EchoTcpTransportListener));

        public EchoTcpTransportListener(ITransportManager transportManager) : base("ECHO", transportManager)
        {
            //ProcessConnect = ProcessEchoConnect;
        }

        protected override TcpTransportConnection ProcessConnect(ITcpOverUdptSocket socket)
        {
            var connection = new DefaultTcpTransportConnection(socket, Reader);
            return connection;
        }

        private bool Reader(byte[] data, int length, TcpTransportConnection connection)
        {
            Logger.Debug("Got some data to echo " + Encoding.UTF8.GetString(data,0,length));
            return connection.SendDataToTransportSocket(data, length);
        }
    }
}
