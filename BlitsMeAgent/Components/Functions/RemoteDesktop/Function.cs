using System;
using System.Diagnostics;
using BlitsMe.Agent.Components.Chat;
using BlitsMe.Agent.Components.Functions.API;
using BlitsMe.Agent.Components.Functions.FileSend;
using BlitsMe.Agent.Components.Notification;
using BlitsMe.Cloud.Messaging.API;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using log4net;

namespace BlitsMe.Agent.Components.Functions.RemoteDesktop
{
    internal delegate void RDPSessionRequestResponseEvent(object sender, RDPSessionRequestResponseArgs args);
    internal delegate void RDPIncomingRequestEvent(object sender, RDPIncomingRequestArgs args);

    class Function : IFunction
    {
        private readonly BlitsMeClientAppContext _appContext;
        private readonly Engagement _engagement;

        private static readonly ILog Logger = LogManager.GetLogger(typeof (FileSend.Function));

        internal Function(BlitsMeClientAppContext appContext, Engagement engagement)
        {
            _appContext = appContext;
            _engagement = engagement;
        }
        
        // event handler to get an acceptance of RDP Session
        internal event RDPSessionRequestResponseEvent RDPSessionRequestResponse;
        internal event RDPIncomingRequestEvent RDPIncomingRequestEvent;

        internal event EventHandler RDPConnectionAccepted { add { Server.ConnectionAccepted += value; } remove { Server.ConnectionAccepted -= value; } }
        internal event EventHandler RDPConnectionClosed { add { Server.ConnectionClosed += value; } remove { Server.ConnectionClosed -= value; } }

        internal void OnRDPIncomingRequestEvent(RDPIncomingRequestArgs args)
        {
            RDPIncomingRequestEvent handler = RDPIncomingRequestEvent;
            if (handler != null) handler(this, args);
        }

        internal void OnRDPSessionRequestResponse(RDPSessionRequestResponseArgs args)
        {
            RDPSessionRequestResponseEvent handler = RDPSessionRequestResponse;
            if (handler != null) handler(this, args);
        }

        internal void ProcessIncomingRDPRequest(String shortCode)
        {
            // Set the shortcode, to make sure we connect to the right caller.
            _engagement.SecondParty.ShortCode = shortCode;
            RDPNotification rdpNotification = new RDPNotification() { Message = _engagement.SecondParty.Name + " would like to access your desktop.", From = _engagement.SecondParty.Username };
            rdpNotification.ProcessDenyRDP += RDPNotificationOnProcessDenyRDP;
            rdpNotification.ProcessAcceptRDP += RDPNotificationOnProcessAcceptRDP;
            _appContext.NotificationManager.AddNotification(rdpNotification);
            _engagement.Chat.LogSystemMessage(_engagement.SecondParty.Name + " sent you a request to control your desktop.");
            OnRDPIncomingRequestEvent(new RDPIncomingRequestArgs(_engagement));
        }

        private void RDPNotificationOnProcessAcceptRDP(object sender, EventArgs eventArgs)
        {
            _engagement.Chat.LogSystemMessage("You accepted the desktop assistance request from " + _engagement.SecondParty.Name);
            try
            {
                if (_appContext.BlitsMeServiceProxy.tvncStartService())
                {
                    Server.Start();
                }
                else
                {
                    Logger.Error("Failed to start the TVN Service. FIXME - not enough info.");
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to start server : " + e.Message, e);
            }
            RDPRequestResponseRq request = new RDPRequestResponseRq() { accepted = true, shortCode = _engagement.SecondParty.ShortCode, username = _engagement.SecondParty.Username };
            try
            {
                _appContext.ConnectionManager.Connection.RequestAsync(request, delegate { });
            }
            catch (Exception e)
            {
                Logger.Error("Failed to send a RDP acceptance request to " + _engagement.SecondParty.Username, e);
            }
        }

        private void RDPNotificationOnProcessDenyRDP(object sender, EventArgs eventArgs)
        {
            _engagement.Chat.LogSystemMessage("You denied the desktop assistance request from " + _engagement.SecondParty.Name);
            RDPRequestResponseRq request = new RDPRequestResponseRq() { accepted = false, shortCode = _engagement.SecondParty.ShortCode, username = _engagement.SecondParty.Username };
            try
            {
                _appContext.ConnectionManager.Connection.RequestAsync(request, delegate {  });
            }
            catch (Exception e)
            {
                Logger.Error("Failed to send a RDP denial request to " + _engagement.SecondParty.Username, e);
            }
        }
        private Client _client;
        public Client Client
        {
            get
            {
                if (_client == null)
                {
                    _client = new Client(_engagement.TransportManager);
                }
                return _client;
            }
        }

        private Server _server;
        public Server Server
        {
            get
            {
                if (_server == null)
                {
                    _server = new Server(_engagement.TransportManager);
                    _server.ConnectionClosed += ServerOnConnectionClosed;
                }
                return _server;
            }
        }

        private void ServerOnConnectionClosed(object sender, EventArgs eventArgs)
        {
#if DEBUG
            Logger.Debug("Server connection closed, notifying end of service.");
#endif
            _engagement.Chat.LogServiceCompleteMessage("You were just helped by " + _engagement.SecondParty.Name + ", please rate his service below.");
        }

        internal void RequestRDPSession()
        {
            RDPRequestRq request = new RDPRequestRq() { shortCode = _engagement.SecondParty.ShortCode, username = _engagement.SecondParty.Username };
            try
            {
                ChatElement chatElement = _engagement.Chat.LogSystemMessage("You sent " + _engagement.SecondParty.Name + " a request to control their desktop.");
                _appContext.ConnectionManager.Connection.RequestAsync(request, (req, res) => ProcessRequestRDPSessionResponse(req, res, chatElement));
            }
            catch (Exception ex)
            {
                Logger.Error("Error during request for RDP Session : " + ex.Message, ex);
                _engagement.Chat.LogSystemMessage("An error occured trying to send " + _engagement.SecondParty.Name + " a request to control their desktop.");
            }
        }

        private void ProcessRequestRDPSessionResponse(Request request, Response response, ChatElement chatElement)
        {
            if (response is ErrorRs || !response.isValid())
            {
                Logger.Error("Received a async response to " + request.id + " that is an error");
                chatElement.DeliveryState = ChatDeliveryState.Failed;
            }
            else
            {
                chatElement.DeliveryState = ChatDeliveryState.Delivered;
            }
        }

        internal void ProcessRemoteDesktopRequestResponse(RDPRequestResponseRq request)
        {
            if (request.accepted)
            {
                _engagement.Chat.LogSystemMessage(_engagement.SecondParty.Name + " accepted your remote assistance request.");
                try
                {
                    int port = Client.Start();
                    Process.Start("c:\\Program Files\\TightVNC\\tvnviewer.exe", "127.0.0.1:" + port);
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to start RDP client : " + e.Message, e);
                    throw e;
                }
            }
            else
            {
                _engagement.Chat.LogSystemMessage(_engagement.SecondParty.Name + " did not accept your remote assistance request.");
            }
        }
    }
}
