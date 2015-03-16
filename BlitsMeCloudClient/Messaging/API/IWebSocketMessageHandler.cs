using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Bauglir.Ex;

namespace Gwupe.Cloud.Messaging.API
{
    public interface IWebSocketMessageHandler
    {
        void OnClose(WebSocketConnection aConnection, int aCloseCode, string aCloseReason, bool aClosedByPeer);
        void OnOpen(WebSocketConnection aConnection);
        void ProcessMessage(String message);
    }
}
