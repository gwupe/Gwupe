using System;
using BlitsMe.Communication.P2P.RUDP.Connector.API;
using BlitsMe.Communication.P2P.RUDP.Socket.API;

namespace BlitsMe.Communication.P2P.RUDP.Connector
{
    public delegate bool ProcessTransportRead(byte[] data, TcpTransportConnection connection);

    public class DefaultTcpTransportConnection : TcpTransportConnection
    {
        private readonly ProcessTransportRead _reader;

        public DefaultTcpTransportConnection(ITcpOverUdptSocket socket, ProcessTransportRead reader) : base(socket)
        {
            _reader = reader;
        }

        protected override void _Start()
        {
        }

        protected override void _Close(bool initiatedBySelf)
        {
        }

        protected override bool ProcessTransportRead(byte[] data)
        {
            return _reader(data, this);
        }
    }
}