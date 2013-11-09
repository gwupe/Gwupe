using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using Bauglir.Ex;
using BlitsMe.Cloud.Communication;
using BlitsMe.Cloud.Exceptions;
using BlitsMe.Cloud.Messaging.API;
using log4net;

namespace BlitsMe.Cloud.Messaging
{
    public class WebSocketMessageHandler : IWebSocketMessageHandler
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(WebSocketMessageHandler));

        private WebSocketConnection _connection;
        private readonly ConnectionMaintainer _connectionMaintainer;
        public WebSocketClient WebSocketClient { get; private set; }
        public WebSocketServer WebSocketServer { get; private set; }
        private MemoryStream _messageBuffer;


        public WebSocketMessageHandler(ConnectionMaintainer cm)
        {
            this._connectionMaintainer = cm;
            WebSocketClient = new WebSocketClient(this);
            WebSocketServer = new WebSocketServer(this);
        }

        public void OnClose(WebSocketConnection aConnection, int aCloseCode, string aCloseReason, bool aClosedByPeer)
        {
            Logger.Debug("Client : Connection [" + aConnection.ToString() + "] has closed with message : " + aCloseReason);
            WebSocketClient.Reset();
            WebSocketServer.Reset();
            this._connection = null;
            this._connectionMaintainer.WakeupManager.Set();
        }

        public void OnOpen(WebSocketConnection aConnection)
        {
            this._connection = aConnection;
            Logger.Debug("Client : Made connection [" + aConnection.Client.Client.RemoteEndPoint + "]");
        }

        /*
        public void onMessage(WebSocketConnection connection, bool final, bool res1, bool res2, bool res3, int code, MemoryStream data)
        {
            // Deserialise it once to get its type
            Message message;
            try
            {
                message = GetMessage(data, final, code);
                if (message == null)
                {
                    return;
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to process message from json into an object : " + e.Message);
                return;
            }
            ProcessMessage(message);
        }
         */

        private void ProcessMessage(Message message)
        {
            if (message is API.Response)
            {
                WebSocketClient.ProcessResponse((API.Response) message);
            }
            else if (message is API.Request)
            {
                WebSocketServer.ProcessRequest((API.Request) message);
            }
            else
            {
                Logger.Error("Failed to determine message type for message " + message.id);
            }
        }

        public void SendMessage(Message message)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(message.GetType());
            MemoryStream stream = new MemoryStream();
            ser.WriteObject(stream, message);
            String resAsString = Encoding.UTF8.GetString(stream.ToArray());
            _connection.SendText(resAsString);
            //if (!Regex.Match(resAsString, "\"type\":\"Ping").Success)
            //{
                Logger.Debug("Sent message : " + SanitiseMessage(resAsString));
            //}
        }

        private Message GetMessage(MemoryStream rawData, bool finalFrame, int opCode)
        {
            MemoryStream data = null;
            if (finalFrame)
            {

                // This is a final frame
                if (opCode == 0)
                {
                    // This is the final frame of a multi framed message
                    _messageBuffer.Write(rawData.GetBuffer(), 0, (int)rawData.Length);
                    data = _messageBuffer;
                    data.Position = 0;
                }
                else
                {
                    // This frame is a single frame
                    data = rawData;
                }
                String messageString = Encoding.UTF8.GetString(data.ToArray());
                //if (!Regex.Match(messageString, "\"type\":\"Ping").Success)
                //{
                    Logger.Debug("Received message [" + data.Length + "] (" + (opCode == 0 ? "multi" : "single") + "): " +
                                 SanitiseMessage(messageString));
                //}
            }
            else
            {
                // This is a multi framed message
                if (opCode != 0)
                {
                    // This is the first frame of the multi framed message
                    _messageBuffer = new MemoryStream();
                    rawData.WriteTo(_messageBuffer);
                    return null;
                }
                // This is an intemediary frame of a multi framed message
                // Add it to the buffer and return
                _messageBuffer.Write(rawData.GetBuffer(), 0, (int)rawData.Length);
                return null;
            }
            return ParseDataToMessage(data);
        }

        private Message ParseDataToMessage(MemoryStream data)
        {
            DataContractJsonSerializer detectSerializer = new DataContractJsonSerializer(typeof (MessageImpl));
            MemoryStream copyData = new MemoryStream();
            data.CopyTo(copyData);
            data.Position = copyData.Position = 0;
            MessageImpl messageDetect = (MessageImpl) detectSerializer.ReadObject(copyData);
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
                Message message = (Message) messageSerializer.ReadObject(data);
                return message;
            }
            catch (Exception e)
            {
                throw new ProtocolException("Failed to deserialize message : " + e.Message);
            }
        }

        private string SanitiseMessage(string messageString)
        {
            return Regex.Replace(Regex.Replace(messageString, "\"password\":\".*?\"", "\"password\":\"*******\""),
                "\"([^\"]+)\":\"([^\"]{255}.*?)\"", "\"$1\":\"<LARGE_DATA>\"");
        }

        public void ProcessMessage(String s)
        {
            if (!Regex.Match(s, "\"type\":\"Ping").Success)
            {
                Logger.Debug("Received message [" + s.Length + "] : " + SanitiseMessage(s));
            }
            // Deserialise it once to get its type
            Message message;
            try
            {
                MemoryStream data = new MemoryStream(Encoding.UTF8.GetBytes(s));
                message = ParseDataToMessage(data);
                if (message == null)
                {
                    return;
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to process message from json into an object : " + e.Message);
                return;
            }
            ProcessMessage(message);
        }

    }
}
