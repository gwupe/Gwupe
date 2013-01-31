using System;
using System.Collections.Generic;
using BlitsMe.Cloud.Messaging.Response;
using log4net;

namespace BlitsMe.Cloud.Messaging
{
    public delegate TRs ProcessRequest<in TRq, out TRs>(TRq request) where TRq : API.Request where TRs : API.Response;

    public class WebSocketServer
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (WebSocketServer));
        private readonly WebSocketMessageHandler _messageHander;

        // Incoming message handlers
        private readonly Dictionary<String, Delegate> _processHandlers;

        public WebSocketServer(WebSocketMessageHandler handler)
        {
            this._messageHander = handler;
            _processHandlers = new Dictionary<string, Delegate>();
        }

        public void reset()
        {
        }

        public void RegisterProcessHandler(string name, Delegate handler)
        {
            _processHandlers.Add(name, handler);
        }
        private void SendResponse(API.Response response, API.Request request)
        {
            response.id = request.id;
            response.date = DateTime.Now;
            try
            {
                _messageHander.sendMessage(response);
            }
            catch (Exception e)
            {
                Logger.Info("Failed to send response for request " + request.id + " : " + e.Message);
            }
        }

        public void ProcessRequest(API.Request request)
        {
            String requestType = request.GetType().Name;
            String processorName = requestType.Substring(0, requestType.Length - 2);
            API.Response response = null;
            // Do we have a process method for this processor
            if (_processHandlers.ContainsKey(processorName))
            {
                try
                {
                    Type responseType = Type.GetType("BlitsMe.Cloud.Messaging.Response." + processorName + "Rs");
                    System.Delegate myDelegate = _processHandlers[processorName];
                    try
                    {
                        response = (API.Response) myDelegate.DynamicInvoke(request);
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Failed to process message : " + e.Message, e);
                        response = (API.Response) responseType.GetConstructor(Type.EmptyTypes).Invoke(new object[] {});
                        response.error = "UNKNOWN_ERROR";
                        response.errorMessage = e.Message;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn("Failed to find to process request of type " + processorName + " : " + ex.Message, ex);
                    response = new ErrorRs(request.id, "Failed to process request of type " + processorName);
                }
            }
            else
            {
                Logger.Warn("Failed to find a processor for " + processorName);
                response = new ErrorRs(request.id, "Failed to find a processor for " + processorName);
            }
            SendResponse(response, request);
        }
    }
}
