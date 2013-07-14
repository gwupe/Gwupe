using System;
using BlitsMe.Communication.P2P.RUDP.Socket.API;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;
using BlitsMe.Communication.P2P.RUDP.Utils;
using log4net;

namespace BlitsMe.Communication.P2P.RUDP.Socket
{
    class BufferedTcpOverUdptSocket : IInternalTcpOverUdptSocket
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(BufferedTcpOverUdptSocket));
        private ITcpTransportLayer _connection;
        private CircularBuffer<byte> clientBuffer;
        public byte ConnectionId { get { return _connection.ConnectionId; } }

        public bool Closing { get; private set; }

        public BufferedTcpOverUdptSocket(ITcpTransportLayer connection)
        {
            _connection = connection;
            clientBuffer = new CircularBuffer<byte>(32768);
        }

        public ITcpTransportLayer Connection
        {
            get { return _connection; }
        }

        public void Send(byte[] data, int length, int timeout)
        {
            Connection.SendData(data, length, timeout);
        }

        public int Read(byte[] data, int maxRead)
        {
            if (!Closing && !Closed)
            {
                int returnValue = clientBuffer.Get(data, maxRead);
#if DEBUG
                Logger.Debug("[" + ConnectionId + "] Read " + returnValue + " bytes from transport buffer, buffSize=" + clientBuffer.Count);
#endif
                return returnValue;
            }
            else
            {
                throw new ObjectDisposedException("[" + ConnectionId + "] Socket has been closed");
            }
        }

        public bool Closed { get; private set; }

        public void Close()
        {
            if (!Closing && !Closed)
            {
                Closing = true;
                clientBuffer.Release();
                _connection.Close();
                Closing = false;
                Closed = true;
            }
        }

        public int BufferClientData(byte[] data)
        {
            try
            {
                clientBuffer.Add(data, data.Length, 10000);
#if DEBUG
                Logger.Debug("[" + ConnectionId + "] Added " + data.Length + " bytes to client buffer, buffSize=" + clientBuffer.Count);
#endif
                return clientBuffer.Available;
            }
            catch (Exception e)
            {
                Logger.Error("[" + ConnectionId + "] Failed to buffer data from tcp layer : " + e.Message, e);
                Close();
            }
            return 0;
        }
    }
}
