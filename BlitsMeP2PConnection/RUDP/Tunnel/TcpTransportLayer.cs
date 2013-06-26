using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using BlitsMe.Communication.P2P.RUDP.Packet.API;
using BlitsMe.Communication.P2P.RUDP.Packet.TCP;
using BlitsMe.Communication.P2P.RUDP.Socket;
using BlitsMe.Communication.P2P.RUDP.Socket.API;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;
using log4net;
using log4net.Repository.Hierarchy;

namespace BlitsMe.Communication.P2P.RUDP.Tunnel
{
    public abstract class TcpTransportLayer : ITcpTransportLayer
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (TcpTransportLayer));

        private readonly byte _connectionId;
        protected ITCPTransport Transport;
        private readonly byte _remoteConnectionId;
        private bool _isEstablished;
        protected readonly Object CheckEstablishedLock = new Object();
        public abstract byte ProtocolId { get; }
        public IInternalTcpOverUdptSocket Socket { get; private set; }
        public bool Established { get { return _isEstablished; } }
        public bool Closing { protected set; get; }
        public bool Closed { protected set; get; }
        public abstract void ProcessDataPacket(ITcpPacket packet);
        public abstract void SendData(byte[] data, int timeout);
        public abstract void ProcessAck(StandardAckPacket packet);

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


        public void _Close()
        {
            if (_isEstablished)
            {
                _isEstablished = false;
                // close the socket
                Socket.Close();
                // close the connection maintained by the transportManager
                Transport.CloseConnection(_connectionId);
            }
        }


        public void Open()
        {
            lock (CheckEstablishedLock)
            {
                _isEstablished = true;
                Monitor.Pulse(CheckEstablishedLock);
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
#if(DEBUG)
                    Logger.Debug("Connection [" + ConnectionId + "] not yet established, waiting for connection");
#endif
                    Monitor.Wait(CheckEstablishedLock, timeout);
#if(DEBUG)
                    Logger.Debug("Connection [" + ConnectionId + "] established, continuing to send data");
#endif
                }
            }
        }
    }
}
