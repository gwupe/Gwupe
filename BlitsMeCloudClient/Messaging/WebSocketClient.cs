using System;
using System.Collections.Generic;
using System.IO;
using BlitsMe.Cloud.Exceptions;
using BlitsMe.Cloud.Messaging.Response;
using System.Threading;
using log4net;

namespace BlitsMe.Cloud.Messaging
{
    public class WebSocketClient
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(WebSocketClient));

        private readonly WebSocketMessageHandler _messageHandler;
        private Dictionary<String, API.Response> _responseStore;
        private Dictionary<String, AutoResetEvent> _responseWaiters;
        private const int WaitTime = 60000;

        public WebSocketClient(WebSocketMessageHandler handler)
        {
            this._messageHandler = handler;
            this.Reset();
        }

        public void Reset()
        {
            this._responseStore = new Dictionary<string, API.Response>();
            this._responseWaiters = new Dictionary<string, AutoResetEvent>();
            
        }

        public TRs SendRequest<TRq, TRs>(TRq request) where TRq : API.Request where TRs : API.Response
        {
            request.date = DateTime.Now;
            try
            {
                _messageHandler.SendMessage(request);
            }
            catch (Exception e)
            {
                throw new IOException("Failed to send request to server : " + e.Message,e);
            }
            API.Response response = AwaitResponse(request);
            if (response is ErrorRs)
            {
                Logger.Error("Error sending message [" + request.id + " - " + request.type + "] : [" + response.error + "] " + response.errorMessage);
                throw new RemoteException(response.errorMessage, response.error);
            }
            if (!response.isValid())
            {
                Logger.Error("Messaging error sending message [" + request.id + " - " + request.type + "] : [" + response.error + "] " + response.errorMessage);
                throw new MessageException<TRs>((TRs)response);
            }
            return (TRs)response;
        }

        private API.Response AwaitResponse(API.Request request)
        {
            API.Response response;
            AutoResetEvent waitEvent = new AutoResetEvent(false);
            if (!_responseStore.ContainsKey(request.id))
            {
                _responseWaiters.Add(request.id, waitEvent);
                waitEvent.WaitOne(WaitTime);
                _responseWaiters.Remove(request.id);
            }

            if (_responseStore.ContainsKey(request.id))
            {
                response = _responseStore[request.id];
                _responseStore.Remove(request.id);
            }
            else
            {
                throw new TimeoutException("Response not received within timeout period");
            }
            return response;
        }

        public void ProcessResponse(API.Response response)
        {
            _responseStore.Add(response.id, response);
            // Signal the wait handler
            try
            {
                _responseWaiters[response.id].Set();
            }
            catch (KeyNotFoundException e)
            {
                Logger.Info("No event handler found to message [" + response.id + "] : " + e.Message);
            }
        }
    }
}
