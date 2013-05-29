using System;
using System.Collections.Generic;
using System.Threading;
using BlitsMe.Cloud.Messaging.API;
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
        private readonly Dictionary<String, Processor> _processors;

        public WebSocketServer(WebSocketMessageHandler handler)
        {
            this._messageHander = handler;
            _processors = new Dictionary<string, Processor>();
        }

        public void RegisterProcessor(string processName, Processor processor)
        {
            _processors.Add(processName, processor);
        }

        public void Reset()
        {
            
        }

        private void SendResponse(API.Response response, API.Request request)
        {
            response.id = request.id;
            response.date = DateTime.Now;
            try
            {
                _messageHander.SendMessage(response);
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
            Processor processor;
            if(_processors.TryGetValue(processorName, out processor))
            {
                ThreadPool.QueueUserWorkItem(delegate { RunProcessor(request, processor, processorName); });
            }
            else
            {
                Logger.Error("Failed to find a processor for " + processorName);
                SendResponse(new ErrorRs() { error = "INTERNAL_SERVER_ERROR", errorMessage = "Failed to find a processor for " + processorName },request);
            }
        }

        private void RunProcessor(API.Request request, Processor processor, string processorName)
        {
            API.Response response = null;
            try
            {
                // Threadpooling
                response = processor.process(request);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to process message with processor " + processor.GetType() + " : " + e.Message, e);
                try
                {
                    Type responseType = Type.GetType("BlitsMe.Cloud.Messaging.Response." + processorName + "Rs");
                    response = (API.Response) responseType.GetConstructor(Type.EmptyTypes).Invoke(new object[] {});
                    response.error = "UNKNOWN_ERROR";
                    response.errorMessage = e.Message;
                }
                catch (Exception exception)
                {
                    Logger.Error("Failed to determine return type for " + processorName);
                    response = new ErrorRs
                        {
                            errorMessage = "Failed to determine return type for " + processorName,
                            error = "INTERNAL_SERVER_ERROR"
                        };
                }
            }
            finally
            {
                SendResponse(response, request);
            }
        }
    }
}
