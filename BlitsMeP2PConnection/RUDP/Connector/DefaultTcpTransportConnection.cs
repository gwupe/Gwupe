using System;
using BlitsMe.Communication.P2P.RUDP.Connector.API;
using BlitsMe.Communication.P2P.RUDP.Socket.API;

namespace BlitsMe.Communication.P2P.RUDP.Connector
{
    public delegate bool ProcessTransportRead(byte[] data, int length, TcpTransportConnection connection);

    public class DefaultTcpTransportConnection : TcpTransportConnection
    {
        private readonly ProcessTransportRead _reader;

        public DefaultTcpTransportConnection(ITcpOverUdptSocket socket, ProcessTransportRead reader) : base(socket)
        {
            _reader = reader;
            CompleteInit();
        }

        protected override void _Close()
        {
        }

        protected override bool ProcessTransportSocketRead(byte[] data, int length)
        {
            return _reader(data, length, this);
        }
    }
}