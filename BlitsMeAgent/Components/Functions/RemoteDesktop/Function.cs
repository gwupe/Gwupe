using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using BlitsMe.Agent.Components.Chat;
using BlitsMe.Agent.Components.Functions.API;
using BlitsMe.Agent.Components.Functions.FileSend;
using BlitsMe.Agent.Components.Functions.RemoteDesktop.Notification;
using BlitsMe.Agent.Components.Notification;
using BlitsMe.Cloud.Messaging.API;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using log4net;
using System.Runtime.InteropServices;



namespace BlitsMe.Agent.Components.Functions.RemoteDesktop
{
    internal delegate void RDPSessionRequestResponseEvent(object sender, RDPSessionRequestResponseArgs args);
    internal delegate void RDPIncomingRequestEvent(object sender, RDPIncomingRequestArgs args);

    class Function : IFunction
    {
        [DllImportAttribute("User32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImportAttribute("User32.dll")]
        static extern bool SwitchToThisWindow(IntPtr hWnd, bool fAltTab);
        [DllImportAttribute("User32.dll")]
        static extern bool IsWindow(IntPtr hWnd);

        private readonly BlitsMeClientAppContext _appContext;
        private readonly Engagement _engagement;

        private static readonly ILog Logger = LogManager.GetLogger(typeof(FileSend.Function));
        private Process _bmssHandle = null;

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
            bool notificationExists = false;
            // Loop through existing notifications to see if we already have a remote desktop request
            // from the SecondParty. If the .From and Type of the notification match the SecondParty.UserName
            // and RDPNotification type
            foreach (Components.Notification.Notification n in _appContext.NotificationManager.Notifications)
            {
                if (n.AssociatedUsername == _engagement.SecondParty.Username && n is RDPNotification)
                {
                    // if the notification exists set flag.
                    notificationExists = true;
                }
            }
            if (!notificationExists)
            {
                if (!Server.Established)
                {
                    if (Server.Listening)
                    {
                        Server.Close();
                    }
                    // only process this notification if they don't already exist AND 
                    // we are not currently in a remote session
                    IsActive = true;
                    // Set the shortcode, to make sure we connect to the right caller.
                    _engagement.SecondParty.ShortCode = shortCode;
                    RDPNotification rdpNotification = new RDPNotification()
                        {
                            Message = _engagement.SecondParty.Name + " would like to access your desktop.",
                            AssociatedUsername = _engagement.SecondParty.Username,
                            DeleteTimeout = 300
                        };
                    rdpNotification.AnsweredTrue += delegate { ProcessAnswer(true); };
                    rdpNotification.AnsweredFalse += delegate { ProcessAnswer(false); };
                    _appContext.NotificationManager.AddNotification(rdpNotification);
                    _engagement.Chat.LogSystemMessage(_engagement.SecondParty.Firstname +
                                                      " requested control of your desktop.");
                    OnRDPIncomingRequestEvent(new RDPIncomingRequestArgs(_engagement));
                }
            }
        }

        // Called when the users accepts or denies
        private void ProcessAnswer(bool accept)
        {
            if (accept)
            {
                _engagement.Chat.LogSystemMessage("You accepted the desktop assistance request from " +
                                                  _engagement.SecondParty.Name);
                try
                {
                    if (_appContext.BlitsMeServiceProxy.VNCStartService())
                    {
                        Server.Listen();
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
                SendRDPRequestResponse(true, delegate(RDPRequestResponseRq rq, RDPRequestResponseRs rs, Exception arg3) { IsActive = arg3 == null; });
            }
            else
            {
                _engagement.Chat.LogSystemMessage("You denied the desktop assistance request from " + _engagement.SecondParty.Name);
                SendRDPRequestResponse(false, delegate(RDPRequestResponseRq rq, RDPRequestResponseRs rs, Exception arg3) { IsActive = false; });
            }
        }

        private void SendRDPRequestResponse(bool answer, Action<RDPRequestResponseRq, RDPRequestResponseRs, Exception> handler)
        {
            RDPRequestResponseRq request = new RDPRequestResponseRq()
                {
                    accepted = answer,
                    shortCode = _engagement.SecondParty.ShortCode,
                    username = _engagement.SecondParty.Username
                };
            try
            {
                _appContext.ConnectionManager.Connection.RequestAsync<RDPRequestResponseRq, RDPRequestResponseRs>(
                    request, handler);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to send a RDP request (answer=" + answer + ") to " + _engagement.SecondParty.Username, e);
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
                    _client.ConnectionAccepted += ClientOnConnectionAccepted;
                    _client.ConnectionClosed += ClientOnConnectionClosed;
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
                    _server.ConnectionAccepted += ServerOnConnectionAccepted;
                    _server.ConnectionClosed += ServerOnConnectionClosed;
                }
                return _server;
            }
        }

        private void ServerOnConnectionAccepted(object sender, EventArgs eventArgs)
        {
            Logger.Info("The remote party has connected to the RDP server");
        }

        // Client disconnected or we kicked him off muhahaha!
        private void ServerOnConnectionClosed(object sender, EventArgs eventArgs)
        {
            IsActive = false;
            Logger.Info("Server connection closed, notifying end of service.");
            _engagement.Chat.LogServiceCompleteMessage("You were just helped by " + _engagement.SecondParty.Name + ", please rate his service below.");
        }

        // Called When we send a rdp request
        internal void RequestRDPSession()
        {
            // Added: JH 2013-04-26
            // _bmssHandle stores the process information of the launched bmss process
            // if that is null - no session is in progress otherwise check that window is open and that its title contains BlitsMe

            if (_bmssHandle != null && !_bmssHandle.HasExited && IsWindow(_bmssHandle.MainWindowHandle) && _bmssHandle.MainWindowTitle.Contains("BlitsMe"))
            {
                // First call SwitchToThisWindow to unminimize it if it is minimized
                SwitchToThisWindow(_bmssHandle.MainWindowHandle, true);
                // Then foreground it so that it is at the front.
                SetForegroundWindow(_bmssHandle.MainWindowHandle);
                // we are done - window acticated nothing more to do so bug out
                return;
            }
            // if we get this far then _bmssHandle is not valid so null it to be sure.
            _bmssHandle = null;
            RDPRequestRq request = new RDPRequestRq() { shortCode = _engagement.SecondParty.ShortCode, username = _engagement.SecondParty.Username };
            try
            {
                ChatElement chatElement = _engagement.Chat.LogSystemMessage("You sent " + _engagement.SecondParty.Name + " a request to control their desktop.");
                _appContext.ConnectionManager.Connection.RequestAsync<RDPRequestRq, RDPRequestRs>(request, (req, res, ex) => ProcessRequestRDPSessionResponse(req, res, ex, chatElement));
            }
            catch (Exception ex)
            {
                Logger.Error("Error during request for RDP Session : " + ex.Message, ex);
                _engagement.Chat.LogSystemMessage("An error occured trying to send " + _engagement.SecondParty.Name + " a request to control their desktop.");
            }
        }

        // Async callback to a RDP Request
        private void ProcessRequestRDPSessionResponse(RDPRequestRq request, RDPRequestRs response, Exception e, ChatElement chatElement)
        {
            if (e != null)
            {
                IsActive = false;
                Logger.Error("Received a async response to " + request.id + " that is an error", e);
                chatElement.DeliveryState = ChatDeliveryState.Failed;
            }
            else
            {
                IsActive = true;
                chatElement.DeliveryState = ChatDeliveryState.Delivered;
            }
        }

        internal void ProcessRemoteDesktopRequestResponse(RDPRequestResponseRq request)
        {
            if (request.accepted)
            {
                IsActive = true;
                _engagement.Chat.LogSystemMessage(_engagement.SecondParty.Name + " accepted your remote assistance request.");
                try
                {
                    int port = Client.Start();
                    String viewerExe = System.IO.Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]) +
                                       "\\bmss.exe";
                    var parameters = "-username=\"" + _engagement.SecondParty.Name + "\" -scale=auto 127.0.0.1::" + port;
                    Logger.Debug("Running " + viewerExe + " " + parameters);
                    _bmssHandle = Process.Start(viewerExe, parameters);

                }
                catch (Exception e)
                {
                    IsActive = false;
                    Logger.Error("Failed to start RDP client : " + e.Message, e);
                    throw e;
                }
            }
            else
            {
                IsActive = false;
                _engagement.Chat.LogSystemMessage(_engagement.SecondParty.Name + " did not accept your remote assistance request.");
            }
        }

        private void ClientOnConnectionAccepted(object sender, EventArgs eventArgs)
        {
            Logger.Debug("A RDP client has connected to the proxy.");
        }

        private void ClientOnConnectionClosed(object sender, EventArgs eventArgs)
        {
            Logger.Debug("Client closed its connection");
            IsActive = false;
        }

        private bool _isActive;
        public bool IsActive
        {
            get { return _isActive; }
            private set
            {
                if (_isActive != value)
                {
                    Logger.Debug("RDPFunction is now " + (value ? "Active" : "Inactive"));
                    _isActive = value;
                    if (value)
                        OnActivate(EventArgs.Empty);
                    else
                        OnDeactivate(EventArgs.Empty);
                }
            }
        }

        public event EventHandler Activate;

        public void OnActivate(EventArgs e)
        {
            EventHandler handler = Activate;
            if (handler != null) handler(this, e);
        }

        public event EventHandler Deactivate;

        public void OnDeactivate(EventArgs e)
        {
            EventHandler handler = Deactivate;
            if (handler != null) handler(this, e);
        }
    }
}
