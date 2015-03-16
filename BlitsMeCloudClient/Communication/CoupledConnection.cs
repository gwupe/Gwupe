using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Bauglir.Ex;
using Gwupe.Cloud.Messaging;
using log4net;

namespace Gwupe.Cloud.Communication
{
    public class CoupledConnection
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (CoupledConnection));
        private readonly string _uniqueId;
        private readonly X509Certificate2 _cert;
        private readonly Func<MemoryStream, bool> _readData;
        private WebSocketClientConnection _connection;
        private CoupledWebSocketMessageHandler _messageHandler;

        public bool Closed;
        public bool Closing;
        private Uri _uri;

        public event EventHandler Connected;

        protected virtual void OnConnected()
        {
            EventHandler handler = Connected;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        public event EventHandler Disconnected;

        protected virtual void OnDisconnected()
        {
            EventHandler handler = Disconnected;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        public CoupledConnection(String uniqueId, String address, X509Certificate2 cert, Func<MemoryStream,bool> readData)
        {
            _uniqueId = uniqueId;
            _uri = new Uri("https://" + address + ":443/blitsme-ws/couple");
            _cert = cert;
            _readData = readData;
        }

        public void Connect()
        {
            _messageHandler = new CoupledWebSocketMessageHandler(this);
            _connection = new WebSocketClientSSLConnection(_cert, _messageHandler);
            _connection.ConnectionClose += _messageHandler.OnClose;
            _connection.ConnectionClose += delegate { OnDisconnected(); };
            _connection.ConnectionOpen += _messageHandler.OnOpen;
            _connection.ConnectionOpen += delegate { OnConnected(); };
            _connection.ConnectionReadFull += ProcessStream;
            try
            {
                if (!_connection.Start(_uri.Host, _uri.Port.ToString(), _uri.PathAndQuery, true, "", "message"))
                {
                    throw new IOException("Unknown error connecting to " + _uri);
                }
                _connection.SendText(_uniqueId);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to connect to server [" + _uri + "] : " + e.Message);
                throw new IOException("Failed to connect to server [" + _uri + "] : " + e.Message, e);
            }
        }

        public void Close()
        {
            if (!Closed && !Closing)
            {
                Closing = true;
                if (_connection != null && !_connection.Closed)
                {
                    _connection.Close(WebSocketCloseCode.Normal);
                }
                Closing = false;
                Closed = true;
            }
        }

        public void WriteData(byte[] arrayBytes)
        {
            _connection.SendBinary(new MemoryStream(arrayBytes));
        }

        private void ProcessStream(WebSocketConnection aconnection, int acode, MemoryStream adata)
        {
            Logger.Debug("Received binary message of size " + adata.Length);
            adata.Position = 0;
            if (_readData != null)
            {
                try
                {
                    if (!_readData(adata))
                    {
                        Close();
                    }
                }
                catch (Exception e)
                {
                    Logger.ErrorFormat("Failed to call read data : " + e.Message, e);
                    Close();
                }
            }
        }
    }
}
