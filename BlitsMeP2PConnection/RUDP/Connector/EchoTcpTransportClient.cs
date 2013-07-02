using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using BlitsMe.Communication.P2P.RUDP.Connector.API;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;
using log4net;

namespace BlitsMe.Communication.P2P.RUDP.Connector
{
    public class EchoTcpTransportClient
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (EchoTcpTransportClient));
        private DefaultTcpTransportConnection _transportConnection;
        private AutoResetEvent _waiter;
        private String _echoStore;
        private readonly Object locker = new Object();
        private ITransportManager _transportManager;

        public EchoTcpTransportClient(ITransportManager transportManager)
        {
            _transportManager = transportManager;
        }

        public TcpTransportConnection Connect()
        {
            _transportConnection = new DefaultTcpTransportConnection(_transportManager.TCPTransport.OpenConnection("ECHO"),ReadReply);
            _waiter = new AutoResetEvent(false);
            return _transportConnection;
        }

        public bool SendMessage(String message)
        {
            if (_transportManager != null)
            {
                lock (locker)
                {
                    _echoStore = message;
                    var bytes = Encoding.UTF8.GetBytes(message);
                    _transportConnection.SendDataToTransportSocket(bytes, bytes.Length);
                    if (_waiter.WaitOne(10000))
                    {
                        Logger.Info("Successfully received echo for " + message);
                        return true;
                    }
                    Logger.Info("Timed out waiting for echo for " + message);
                    return false;
                }
            } else
            {
                throw new Exception("Cannot connect, connection not established.");
            }
        }

        private bool ReadReply(byte[] bytes, int length, TcpTransportConnection connection)
        {
            var reply = Encoding.UTF8.GetString(bytes, 0, length);
            if(reply.Equals(_echoStore))
            {
                Logger.Info("Received " + reply);
                _waiter.Set();
            } else
            {
                Logger.Warn("Received unexpected " + reply);
            }
            return true;
        }

        public void Close()
        {
            if (_transportConnection != null) _transportConnection.Close();
        }
    }
}
