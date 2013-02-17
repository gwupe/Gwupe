using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using Bauglir.Ex;
using BlitsMe.Cloud.Communication;
using BlitsMe.Cloud.Exceptions;
using BlitsMe.Cloud.Messaging.API;
using log4net;

namespace BlitsMe.Cloud.Messaging
{
    public class WebSocketMessageHandler
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(WebSocketMessageHandler));

        private WebSocketConnection connection;
        private ConnectionMaintainer _connectionMaintainer;
        public WebSocketClient webSocketClient { get; private set; }
        public WebSocketServer webSocketServer { get; private set; }
        private MemoryStream messageBuffer;


        public WebSocketMessageHandler(ConnectionMaintainer cm)
        {
            this._connectionMaintainer = cm;
            webSocketClient = new WebSocketClient(this);
            webSocketServer = new WebSocketServer(this);
        }

        public void onClose(WebSocketConnection aConnection, int aCloseCode, string aCloseReason, bool aClosedByPeer)
        {
#if DEBUG
            logger.Debug("Client : Connection [" + aConnection.ToString() + "] has closed with message : " + aCloseReason);
#endif
            webSocketClient.Reset();
            webSocketServer.Reset();
            this.connection = null;
            this._connectionMaintainer.wakeupManager.Set();
        }

        public void onOpen(WebSocketConnection aConnection)
        {
            this.connection = aConnection;
#if DEBUG
            logger.Debug("Client : Received connection [" + aConnection.ToString() + "]");
#endif
        }

        public void onMessage(WebSocketConnection connection, bool final, bool res1, bool res2, bool res3, int code, MemoryStream data)
        {
            // Deserialise it once to get its type
            Message message;
            try
            {
                message = getMessage(data,final,code);
                if(message == null)
                {
                    return;
                }
            }
            catch (Exception e)
            {
                logger.Error("Failed to process message from json into an object : " + e.Message);
                return;
            }
            if (message is BlitsMe.Cloud.Messaging.API.Response)
            {
                webSocketClient.ProcessResponse((API.Response)message);
            }
            else if (message is BlitsMe.Cloud.Messaging.API.Request)
            {
                webSocketServer.ProcessRequest((API.Request)message);
            }
            else
            {
                logger.Error("Failed to determine message type for message " + message.id);
            }
        }

        public void sendMessage(API.Message message) {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(message.GetType());
            MemoryStream stream = new MemoryStream();
            ser.WriteObject(stream, message);
            String resAsString = Encoding.UTF8.GetString(stream.ToArray());
            connection.SendText(resAsString);
#if DEBUG
            logger.Debug("Sent message : " + resAsString);
#endif
        }

        private Message getMessage(MemoryStream rawData, bool finalFrame, int opCode)
        {
            MemoryStream data = null;
            if(finalFrame)
            {
                // This is a final frame
                if(opCode == 0)
                {
                    // This is the final frame of a multi framed message
                    messageBuffer.Write(rawData.GetBuffer(), 0, (int)rawData.Length);
                    data = messageBuffer;
#if DEBUG
                        logger.Debug("Full message [" + messageBuffer.Length + "] (multi-complete): " +
                                     Encoding.UTF8.GetString(messageBuffer.ToArray()));
#endif
                    data.Position = 0;
                } else
                {
                    // This frame is a single frame
                    data = rawData;
#if DEBUG
                        logger.Debug("Got message [" + rawData.Length + "] (single): " + Encoding.UTF8.GetString(rawData.ToArray()));
#endif
                }
            } else
            {
                // This is a multi framed message
                if(opCode != 0)
                {
                    // This is the first frame of the multi framed message
                    messageBuffer = new MemoryStream();
                    rawData.WriteTo(messageBuffer);
                    return null;
                } else
                {
                    // This is an intemediary frame of a multi framed message
                    // Add it to the buffer and return
                    messageBuffer.Write(rawData.GetBuffer(), 0, (int)rawData.Length);
                    return null;
                }
            }
            DataContractJsonSerializer detectSerializer = new DataContractJsonSerializer(typeof(MessageImpl));
            MemoryStream copyData = new MemoryStream();
            data.CopyTo(copyData);
            data.Position = copyData.Position = 0;
            MessageImpl messageDetect = (MessageImpl)detectSerializer.ReadObject(copyData);
            String[] typeInfo = messageDetect.type.Split('-');
            String className = "";
            if (typeInfo[1].Equals("RQ"))
            {
                className = "BlitsMe.Cloud.Messaging.Request." + typeInfo[0] + "Rq";
            }
            else if (typeInfo[1].Equals("RS"))
            {
                className = "BlitsMe.Cloud.Messaging.Response." + typeInfo[0] + "Rs";
            }
            else
            {
                throw new ProtocolException("Message type not recognised : " + messageDetect.type);
            }
            try
            {
                Type type = Type.GetType(className);
                DataContractJsonSerializer messageSerializer = new DataContractJsonSerializer(type);
                Message message = (Message)messageSerializer.ReadObject(data);
                return message;
            }
            catch (Exception e)
            {
                throw new ProtocolException("Failed to deserialize message : " + e.Message);
            }
        }



    }
}
