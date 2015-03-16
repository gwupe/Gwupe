using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Bauglir.Ex;
using Gwupe.Cloud.Communication;
using Gwupe.Cloud.Messaging.API;
using log4net;

namespace Gwupe.Cloud.Messaging
{
    public class CoupledWebSocketMessageHandler : IWebSocketMessageHandler
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (CoupledWebSocketMessageHandler));
        private readonly CoupledConnection _coupledConnection;
        private WebSocketConnection _connection;
        private AutoResetEvent greetingEvent = new AutoResetEvent(false);
        private bool greetingResponseOK = false;

        public CoupledWebSocketMessageHandler(CoupledConnection coupledConnection)
        {
            _coupledConnection = coupledConnection;
        }

        public void OnClose(WebSocketConnection aConnection, int aCloseCode, string aCloseReason, bool aClosedByPeer)
        {
            Logger.Debug("Client : Connection [" + aConnection.ToString() + "] has closed with message : " + aCloseReason);
            this._connection = null;
            _coupledConnection.Close();
        }

        public void SendInit(String uniqueId)
        {
            greetingEvent.Reset();
            _connection.SendText(uniqueId);
            if (greetingEvent.WaitOne(30000))
            {
                if (greetingResponseOK)
                    return;
                throw new IOException("Greeting handshake failed");
            }
            throw new IOException("Timed out waiting for greeting answer");
        }

        public void OnOpen(WebSocketConnection aConnection)
        {
            this._connection = aConnection;
            Logger.Debug("Client : Made connection [" + aConnection.Client.Client.RemoteEndPoint + "]");
        }

        public void ProcessMessage(string message)
        {
            greetingResponseOK = message.Equals("OK");
            greetingEvent.Set();
        }

    }
}
