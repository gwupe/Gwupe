using System;
using System.Threading;
using BlitsMe.Communication.P2P.RUDP.Packet.API;
using BlitsMe.Communication.P2P.RUDP.Packet.TCP;
using BlitsMe.Communication.P2P.RUDP.Socket;
using BlitsMe.Communication.P2P.RUDP.Socket.API;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;
using log4net;

namespace BlitsMe.Communication.P2P.RUDP.Tunnel
{
    public abstract class TcpTransportLayer : ITcpTransportLayer
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TcpTransportLayer));

        private readonly byte _connectionId;
        protected ITCPTransport Transport;
        private readonly byte _remoteConnectionId;
        protected readonly Object CheckEstablishedLock = new Object();
        public abstract byte ProtocolId { get; }
        public IInternalTcpOverUdptSocket Socket { get; private set; }
        public bool Established { protected set; get; }
        public abstract ushort LastSeqSent { get; }
        public abstract ushort NextSeqToSend { get; }
        public bool Closing { protected set; get; }
        public bool Closed { protected set; get; }
        public abstract void ProcessDataPacket(ITcpDataPacket tcpDataPacket);
        public abstract void SendData(byte[] data, int length, int timeout);
        public abstract void ProcessAck(StandardAckPacket packet);
        public event EventHandler ConnectionOpen;

        public void OnConnectionOpen(EventArgs e)
        {
            EventHandler handler = ConnectionOpen;
            if (handler != null) handler(this, e);
        }

        public event EventHandler ConnectionClose;
        public abstract void ProcessDisconnect(StandardDisconnectPacket packet);
        public abstract bool Disconnected { get; }

        public void OnConnectionClose(EventArgs e)
        {
            EventHandler handler = ConnectionClose;
            if (handler != null) handler(this, e);
        }

        // Properties
        public byte ConnectionId
        {
            get { return _connectionId; }
        }

        public byte RemoteConnectionId
        {
            get { return _remoteConnectionId; }
        }

        protected TcpTransportLayer(ITCPTransport transport, byte connectionId, byte remoteConnectionId)
        {
            this.Transport = transport;
            this._connectionId = connectionId;
            this._remoteConnectionId = remoteConnectionId;
            Socket = new BufferedTcpOverUdptSocket(this);
        }

        public void Open()
        {
            lock (CheckEstablishedLock)
            {
                Established = true;
                Monitor.Pulse(CheckEstablishedLock);
                OnConnectionOpen(EventArgs.Empty);
            }
        }

        public abstract void Close();

        protected void BlockIfNotEstablished(int timeout)
        {
            lock (CheckEstablishedLock)
            {
                // block if connection is not established
                while (!Established)
                {
                    Logger.Debug("Connection [" + ConnectionId + "] not yet established, waiting for connection");
                    Monitor.Wait(CheckEstablishedLock, timeout);
                    Logger.Debug("Connection [" + ConnectionId + "] established, continuing to send data");
                }
            }
        }
    }
}
