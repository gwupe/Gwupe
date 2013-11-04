using System;
using System.Diagnostics;
using System.Threading;
using BlitsMe.Agent.Components.Functions.API;
using BlitsMe.Agent.Components.Functions.Chat;
using BlitsMe.Agent.Components.Functions.RemoteDesktop.Notification;
using BlitsMe.Agent.Components.Notification;
using BlitsMe.Agent.UI.WPF.Engage;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using log4net;
using System.Runtime.InteropServices;



namespace BlitsMe.Agent.Components.Functions.RemoteDesktop
{
    //internal delegate void RDPSessionRequestResponseEvent(object sender, RdpSessionRequestResponse args);
    //internal delegate void RDPIncomingRequestEvent(object sender, RdpIncomingRequest args);

    class Function : FunctionImpl
    {
        [DllImportAttribute("User32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImportAttribute("User32.dll")]
        static extern bool SwitchToThisWindow(IntPtr hWnd, bool fAltTab);
        [DllImportAttribute("User32.dll")]
        static extern bool IsWindow(IntPtr hWnd);

        private readonly BlitsMeClientAppContext _appContext;
        private readonly Engagement _engagement;
        public  EngagementWindow _EngagementWindow;
        public Engagement _Engagement;
        public override String Name { get { return "RemoteDesktop"; }}

        private static readonly ILog Logger = LogManager.GetLogger(typeof(FileSend.Function));
        private Process _bmssHandle = null;

        private Chat.Function Chat { get { return _engagement.Functions["Chat"] as Chat.Function; } }

        internal Function(BlitsMeClientAppContext appContext, Engagement engagement)
        {
            _appContext = appContext;
            _engagement = engagement;
        }

        // event handler to get an acceptance of RDP Session
        //internal event RDPSessionRequestResponseEvent RDPSessionRequestResponse;
        //internal event RDPIncomingRequestEvent RDPIncomingRequestEvent;

        internal event EventHandler RDPConnectionAccepted { add { Server.ConnectionAccepted += value; } remove { Server.ConnectionAccepted -= value; } }
        internal event EventHandler RDPConnectionClosed { add { Server.ConnectionClosed += value; } remove { Server.ConnectionClosed -= value; } }
        /*
                internal void OnRDPIncomingRequestEvent(RdpIncomingRequest args)
                {
                    RDPIncomingRequestEvent handler = RDPIncomingRequestEvent;
                    if (handler != null) handler(this, args);
                }

                internal void OnRDPSessionRequestResponse(RdpSessionRequestResponse args)
                {
                    RDPSessionRequestResponseEvent handler = RDPSessionRequestResponse;
                    if (handler != null) handler(this, args);
                }
                */

        // Method is called by RequestManager when the second party is requesting and remote desktop session with us
        internal void ProcessIncomingRemoteDesktopRequest(String shortCode)
        {
            bool notificationExists = false;
            // Loop through existing notifications to see if we already have a remote desktop request
            // from the SecondParty. If the .From and Type of the notification match the SecondParty.UserName
            // and RDPNotification type
            foreach (Components.Notification.Notification n in _appContext.NotificationManager.Notifications)
            {
                if (n.AssociatedUsername == _engagement.SecondParty.Person.Username && n is RDPNotification)
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
                    _engagement.SecondParty.ActiveShortCode = shortCode;
                    RDPNotification rdpNotification = new RDPNotification()
                        {
                            Message = _engagement.SecondParty.Person.Name + " would like to access your desktop.",
                            AssociatedUsername = _engagement.SecondParty.Person.Username,
                            DeleteTimeout = 300
                        };
                    rdpNotification.AnsweredTrue += delegate { ProcessAnswer(true); };
                    rdpNotification.AnsweredFalse += delegate { ProcessAnswer(false); };
                    _appContext.NotificationManager.AddNotification(rdpNotification);
                    Chat.LogSecondPartySystemMessage(_engagement.SecondParty.Person.Firstname +
                                                      " requested control of your desktop.");
                    //OnRDPIncomingRequestEvent(new RdpIncomingRequest(_engagement));
                    OnNewActivity(new RemoteDesktopActivity(_engagement, RemoteDesktopActivity.REMOTE_DESKTOP_REQUEST) { To = "_SELF", From = _engagement.SecondParty.Person.Username });
                }
            }
        }

        // Called when the local user accepts or denies the remote desktop request
        private void ProcessAnswer(bool accept)
        {
            if (accept)
            {
                Chat.LogSystemMessage("You accepted the desktop assistance request from " +
                                                  _engagement.SecondParty.Person.Firstname);
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
                Chat.LogSystemMessage("You denied the desktop assistance request from " + _engagement.SecondParty.Person.Firstname);
                SendRDPRequestResponse(false, delegate(RDPRequestResponseRq rq, RDPRequestResponseRs rs, Exception arg3) { IsActive = false; });
            }
            OnNewActivity(new RemoteDesktopActivity(_engagement, RemoteDesktopActivity.REMOTE_DESKTOP_RESPONSE) { To = _engagement.SecondParty.Person.Username, From = "_SELF", Answer = accept });
        }

        // generic method to send a response to the remote desktop request
        private void SendRDPRequestResponse(bool answer, Action<RDPRequestResponseRq, RDPRequestResponseRs, Exception> handler)
        {
            RDPRequestResponseRq request = new RDPRequestResponseRq()
                {
                    accepted = answer,
                    shortCode = _engagement.SecondParty.ActiveShortCode,
                    username = _engagement.SecondParty.Person.Username,
                    interactionId = _engagement.Interactions.CurrentOrNewInteraction.Id,
                };
            try
            {
                _appContext.ConnectionManager.Connection.RequestAsync<RDPRequestResponseRq, RDPRequestResponseRs>(
                    request, handler);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to send a RDP request (answer=" + answer + ") to " + _engagement.SecondParty.Person.Username, e);
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

        // something connected to our server
        private void ServerOnConnectionAccepted(object sender, EventArgs eventArgs)
        {
            Logger.Info("The remote party has connected to the RDP server");
            OnNewActivity(new RemoteDesktopActivity(_engagement, RemoteDesktopActivity.REMOTE_DESKTOP_CONNECT) { From = _engagement.SecondParty.Person.Username, To = "_SELF" });
        }

        // Client disconnected or we kicked him off muhahaha!
        private void ServerOnConnectionClosed(object sender, EventArgs eventArgs)
        {
            IsActive = false;
            Logger.Info("Server connection closed, notifying end of service.");
            Chat.LogServiceCompleteMessage("You were just helped by " + _engagement.SecondParty.Person.Name + ", please rate their service below.");
            OnNewActivity(new RemoteDesktopActivity(_engagement, RemoteDesktopActivity.REMOTE_DESKTOP_DISCONNECT) { From = _engagement.SecondParty.Person.Username, To = "_SELF" });
        }

        // Called When we send a rdp request
        internal void RequestRDPSession(EngagementWindow egw, Engagement eg)
        {
            // Added: JH 2013-04-26
            // _bmssHandle stores the process information of the launched bmss process
            // if that is null - no session is in progress otherwise check that window is open and that its title contains BlitsMe
            
            _Engagement = eg;
            BlitsMeClientAppContext.CurrentAppContext.UIManager.GetRemoteEngagement(_Engagement);
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
            RDPRequestRq request = new RDPRequestRq() { shortCode = _engagement.SecondParty.ActiveShortCode, username = _engagement.SecondParty.Person.Username, interactionId = _engagement.Interactions.CurrentOrNewInteraction.Id };
            try
            {
                ChatElement chatElement = Chat.LogSystemMessage("You sent " + _engagement.SecondParty.Person.Firstname + " a request to control their desktop.");
                _appContext.ConnectionManager.Connection.RequestAsync<RDPRequestRq, RDPRequestRs>(request, (req, res, ex) => ProcessRequestRDPSessionResponse(req, res, ex, chatElement));
            }
            catch (Exception ex)
            {
                Logger.Error("Error during request for RDP Session : " + ex.Message, ex);
                Chat.LogSystemMessage("An error occured trying to send " + _engagement.SecondParty.Person.Firstname + " a request to control their desktop.");
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
                OnNewActivity(new RemoteDesktopActivity(_engagement, RemoteDesktopActivity.REMOTE_DESKTOP_REQUEST) { From = "_SELF", To = _engagement.SecondParty.Person.Username });
            }
        }

        // this is called by the request manager when we receive a answer our remote desktop request (yes or no)
        internal void ProcessRemoteDesktopRequestResponse(RDPRequestResponseRq request)
        {
            OnNewActivity(new RemoteDesktopActivity(_engagement, RemoteDesktopActivity.REMOTE_DESKTOP_RESPONSE) { To = "_SELF", From = _engagement.SecondParty.Person.Username, Answer = request.accepted });
            if (request.accepted)
            {
                IsActive = true;
                Chat.LogSecondPartySystemMessage(_engagement.SecondParty.Person.Firstname + " accepted your remote assistance request, please wait while we establish a connection...");
                RDPDisconnectNotification notification = new RDPDisconnectNotification()
                {
                    Manager = _appContext.NotificationManager,
                    Person = _engagement.SecondParty.Person.Avatar,
                    Message = "TerminateRDP"
                };
                _appContext.NotificationManager.AddNotification(notification);
                _appContext.UIManager.Show();
                // Wait for a tunnel
                lock (_engagement.TunnelWaitLock)
                {
                    Logger.Debug("Testing is tunnel to " + _engagement.SecondParty.Person.Username + " is active");
                    while (!_engagement.IsTunnelActive)
                    {
                        Logger.Debug("Tunnel to " + _engagement.SecondParty.Person.Username + " is not active, waiting");
                        if (!Monitor.Wait(_engagement.TunnelWaitLock, 30000))
                        {
                            Chat.LogErrorMessage("Failed to create a connection to " +
                                                             _engagement.SecondParty.Person.Firstname);
                            Logger.Error("Failed to create a tunnel between " + _appContext.CurrentUserManager.CurrentUser.Name + "(me) and " + _engagement.SecondParty.Person.Username + " : timed out waiting for tunnel");
                            IsActive = false;
                            return;
                        }
                    }
                }
                try
                {
                    int port = Client.Start();
                    String viewerExe = System.IO.Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]) +
                                       "\\bmss.exe";
                    var parameters = "-username=\"" + _engagement.SecondParty.Person.Name + "\" -scale=auto 127.0.0.1::" + port;
                    Logger.Debug("Running " + viewerExe + " " + parameters);
                    _bmssHandle = Process.Start(viewerExe, parameters);

                }
                catch (Exception e)
                {
                    IsActive = false;
                    Logger.Error("Failed to start RDP client to " + _engagement.SecondParty.Person.Username + " : " + e.Message, e);
                    throw e;
                }
            }
            else
            {
                IsActive = false;
                Chat.LogSecondPartySystemMessage(_engagement.SecondParty.Person.Firstname + " did not accept your remote assistance request.");
            }
        }

        private void ClientOnConnectionAccepted(object sender, EventArgs eventArgs)
        {
            Chat.LogSystemMessage("You connected to " + _engagement.SecondParty.Person.Firstname + "'s desktop.");
            Logger.Info("RDP client has connected to the proxy to " + _engagement.SecondParty.Person.Username + ".");
            OnNewActivity(new RemoteDesktopActivity(_engagement, RemoteDesktopActivity.REMOTE_DESKTOP_CONNECT) { From = "_SELF", To = _engagement.SecondParty.Person.Username });
        }

        private void ClientOnConnectionClosed(object sender, EventArgs eventArgs)
        {
            Chat.LogSystemMessage("You disconnected from " + _engagement.SecondParty.Person.Firstname + "'s desktop.");
            Logger.Info("RDP client has disconnected from the proxy to " + _engagement.SecondParty.Person.Username + ".");
            IsActive = false;
            OnNewActivity(new RemoteDesktopActivity(_engagement, RemoteDesktopActivity.REMOTE_DESKTOP_DISCONNECT) { From = "_SELF", To = _engagement.SecondParty.Person.Username });
        }

        public override void Close()
        {
            if (_client != null)
                _client.Close();
            if (_server != null)
                _server.Close();
        }

        public EngagementWindow GetEngageWindow()
        {
            return _EngagementWindow;
        }
    }

    internal class RemoteDesktopActivity : EngagementActivity
    {
        internal const String REMOTE_DESKTOP_REQUEST = "REMOTE_DESKTOP_REQUEST";
        internal const String REMOTE_DESKTOP_RESPONSE = "REMOTE_DESKTOP_REQUEST";
        internal const String REMOTE_DESKTOP_CONNECT = "REMOTE_DESKTOP_REQUEST";
        internal const String REMOTE_DESKTOP_DISCONNECT = "REMOTE_DESKTOP_REQUEST";
        internal bool Answer;

        internal RemoteDesktopActivity(Engagement engagement, String activity)
            : base(engagement, "REMOTE_DESKTOP", activity)
        {
        }

    }
}
