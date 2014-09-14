using System;
using System.Diagnostics;
using System.Threading;
using BlitsMe.Agent.Components.Functions.API;
using BlitsMe.Agent.Components.Functions.Chat;
using BlitsMe.Agent.Components.Functions.Chat.ChatElement;
using BlitsMe.Agent.Components.Functions.RemoteDesktop.ChatElement;
using BlitsMe.Agent.Components.Functions.RemoteDesktop.Notification;
using BlitsMe.Agent.Components.Notification;
using BlitsMe.Agent.UI.WPF.Engage;
using BlitsMe.Cloud.Exceptions;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using BlitsMe.Common.Security;
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
        public override String Name { get { return "RemoteDesktop"; } }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(FileSend.Function));
        private Process _bmssHandle = null;

        private Chat.Function Chat { get { return _engagement.Functions["Chat"] as Chat.Function; } }

        internal Function(BlitsMeClientAppContext appContext, Engagement engagement)
        {
            _appContext = appContext;
            _engagement = engagement;
        }

        // Method is called by RequestManager when the second party is requesting a remote desktop session with us
        internal void ProcessIncomingRemoteDesktopRequest(String shortCode)
        {
            /*
            bool notificationExists = false;
            // Loop through existing notifications to see if we already have a remote desktop request
            // from the SecondParty. If the .From and Type of the notification match the SecondParty.UserName
            // and RDPNotification type
            
            _engagement.IsRemoteControlActive = true;
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
             * */
            // Ignore request if a session is underway
            if (Server.Closed)
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
                // Print to the chat that someone is trying to request control of the desktop (allowing them to click yes/no)
                var rdpChatElement = LogRdpRequest();
                // Setup anser handlers
                rdpChatElement.AnsweredTrue += delegate { ProcessAnswer(true); };
                rdpChatElement.AnsweredFalse += delegate { ProcessAnswer(false); };
                // There has been an activity, raise the event
                OnNewActivity(new RemoteDesktopActivity(_engagement, RemoteDesktopActivity.REMOTE_DESKTOP_REQUEST) { To = "_SELF", From = _engagement.SecondParty.Person.Username });
                // Now we wait to see what the user does
            }
            //}
        }

        // This method prints the message in the chat that someone is requesting a rdp session with us, allowing the user to answer
        internal RdpRequestChatElement LogRdpRequest()
        {
            RdpRequestChatElement chatElement = null;
            String message;
            if (_engagement.SecondParty.Relationship.TheyHaveUnattendedAccess)
            {
                message = _engagement.SecondParty.Person.Firstname +
                          " requested control of your desktop. Unattended access will be granted in 10 seconds.";
                chatElement = new RdpRequestUnattendedChatElement(10)
                {
                    Message = message,
                    SpeakTime = DateTime.Now,
                    UserName = _engagement.SecondParty.Person.Username,
                };

            }
            else
            {
                message = _engagement.SecondParty.Person.Firstname + " requested control of your desktop.";
                chatElement = new RdpRequestChatElement()
                {
                    Message = message,
                    SpeakTime = DateTime.Now,
                    UserName = _engagement.SecondParty.Person.Username,
                };

            }
            Chat.Conversation.AddMessage(chatElement);
            // Notify that there is activity in the chat
            OnNewActivity(new ChatActivity(_engagement, ChatActivity.LOG_RDP_REQUEST)
            {
                From = _engagement.SecondParty.Person.Username,
                To = _appContext.CurrentUserManager.CurrentUser.Username,
                Message = message
            });
            return chatElement;
        }

        // Called when the local user accepts or denies the remote desktop request
        private void ProcessAnswer(bool accept)
        {
            if (accept)
            {
                String connectionId = Util.getSingleton().generateString(16);
                Chat.LogSystemMessage("You accepted the desktop assistance request from " + _engagement.SecondParty.Person.Firstname);
                try
                {
                    // Startup the underlying VNC service
                    if (_appContext.BlitsMeServiceProxy.VNCStartService())
                    {
                        // Now we listen for a connection from the tunnel
                        Server.Listen(connectionId);
                    }
                    else
                    {
                        Logger.Error("Failed to start the VNC Service. FIXME - not enough info.");
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to start server : " + e.Message, e);
                }
                // Notify the second party that we have answered that he can connect
                SendRdpRequestResponse(true, connectionId, delegate(RDPRequestResponseRq rq, RDPRequestResponseRs rs, Exception exception) { IsActive = exception == null; });
            }
            else
            {
                // mark the remote control as not underway
                //_engagement.IsRemoteControlActive = false;
                // Log in the chat that we denied the request
                Chat.LogSystemMessage("You denied the desktop assistance request from " + _engagement.SecondParty.Person.Firstname);
                // notify the second party that he cannot connect.
                SendRdpRequestResponse(false, null, delegate(RDPRequestResponseRq rq, RDPRequestResponseRs rs, Exception arg3) { IsActive = false; });
            }
            OnNewActivity(new RemoteDesktopActivity(_engagement, RemoteDesktopActivity.REMOTE_DESKTOP_RESPONSE) { To = _engagement.SecondParty.Person.Username, From = "_SELF", Answer = accept });
        }

        // generic method to send a response to the remote desktop request
        private void SendRdpRequestResponse(bool answer, String connectionId, Action<RDPRequestResponseRq, RDPRequestResponseRs, Exception> handler)
        {
            // compile the request
            RDPRequestResponseRq request = new RDPRequestResponseRq()
                {
                    accepted = answer,
                    shortCode = _engagement.SecondParty.ActiveShortCode,
                    username = _engagement.SecondParty.Person.Username,
                    interactionId = _engagement.Interactions.CurrentOrNewInteraction.Id,
                    connectionId = connectionId,
                };
            try
            {
                // Send the request asynchronously 
                _appContext.ConnectionManager.Connection.RequestAsync<RDPRequestResponseRq, RDPRequestResponseRs>(
                    request, handler);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to send a RDP request (answer=" + answer + ") to " + _engagement.SecondParty.Person.Username, e);
            }
        }

        // This property is the client (i.e. used to attempt control of second party desktop)
        private Client _client;
        public Client Client
        {
            get
            {
                if (_client == null)
                {
                    _client = new Client(_engagement.SecondParty);
                    _client.ClientConnectionOpened += ClientOnConnectionAccepted;
                    _client.ClientConnectionClosed += ClientOnConnectionClosed;
                }
                return _client;
            }
        }

        // this property is the server (i.e. used to answer the control attempt from the tunnel and do stuff with the traffic)
        private Server _server;
        public Server Server
        {
            get
            {
                if (_server == null)
                {
                    _server = new Server();
                    _server.ServerConnectionOpened += ServerOnConnectionAccepted;
                    _server.ServerConnectionClosed += ServerOnConnectionClosed;
                }
                return _server;
            }
        }

        // This event is fired when someone connects to our server from the tunnel, looking to control our desktop
        private void ServerOnConnectionAccepted(object sender, EventArgs eventArgs)
        {
            Logger.Info("The remote party has connected to the RDP server");
            IsUnderway = true;
            OnNewActivity(new RemoteDesktopActivity(_engagement, RemoteDesktopActivity.REMOTE_DESKTOP_CONNECT) { From = _engagement.SecondParty.Person.Username, To = "_SELF" });
        }

        // This event is fired when the connection to our server is closed
        private void ServerOnConnectionClosed(object sender, EventArgs eventArgs)
        {
            IsActive = false;
            IsUnderway = false;
            Logger.Info("Server connection closed, notifying end of service.");
            if (_engagement.SecondParty.Relationship.TheyHaveUnattendedAccess)
            {
                Chat.LogServiceCompleteMessage("You were just helped by " + _engagement.SecondParty.Person.Name, false);
            }
            else
            {
                Chat.LogServiceCompleteMessage("You were just helped by " + _engagement.SecondParty.Person.Name +
                                               ", please rate their service below.");
            }
            OnNewActivity(new RemoteDesktopActivity(_engagement, RemoteDesktopActivity.REMOTE_DESKTOP_DISCONNECT) { From = _engagement.SecondParty.Person.Username, To = "_SELF" });
        }

        internal void RequestRdpSession()
        {
            if (_engagement.SecondParty.Relationship.IHaveUnattendedAccess)
            {
                ElevatedRequestRdpSession();
            }
            else
            {
                RequestRdpSession(null, null);
            }
        }

        // Called When this user send a rdp request to the second party
        private void RequestRdpSession(string tokenId, string securityKey)
        {
            //BlitsMeClientAppContext.CurrentAppContext.UIManager.GetRemoteEngagement(_engagement);
            // all these checks are to make sure that we aren't currently accessing second partys desktop
            if (CheckRdpActive()) return;
            // now we compile the request to second party to control his desktop
            RDPRequestRq request = new RDPRequestRq()
            {
                shortCode = _engagement.SecondParty.ActiveShortCode,
                username = _engagement.SecondParty.Person.Username,
                interactionId = _engagement.Interactions.CurrentOrNewInteraction.Id,
                securityKey = securityKey,
                tokenId = tokenId,
            };
            try
            {
                // Actually send the message asynchronously
                //_appContext.ConnectionManager.Connection.RequestAsync<RDPRequestRq, RDPRequestRs>(request, (req, res, ex) => ProcessRequestRDPSessionResponse(req, res, ex, chatElement));
                try
                {
                    var response = _appContext.ConnectionManager.Connection.Request<RDPRequestRq, RDPRequestRs>(request);
                    // Print in chat that we sent the second party a rdp request
                    IChatMessage chatElement = Chat.LogSystemMessage("You sent " + _engagement.SecondParty.Person.Firstname +
                        " a request to control their desktop." + (tokenId == null ? "" : "  You have unattended access to their desktop, you will be granted access automatically after 10 seconds."));
                    // The message was delivered
                    IsActive = true;
                    // Raise an activity that we managed to send a rdp request to second party successfully.
                    OnNewActivity(new RemoteDesktopActivity(_engagement, RemoteDesktopActivity.REMOTE_DESKTOP_REQUEST) { From = "_SELF", To = _engagement.SecondParty.Person.Username });
                }
                catch (MessageException<RDPRequestRs> e)
                {
                    if ("WILL_NOT_PROCESS_ELEVATE".Equals(e.ErrorCode) && (tokenId == null && securityKey == null))
                    {
                        // need elevation
                        ElevatedRequestRdpSession();
                    } else if ("WILL_NOT_PROCESS_AUTH".Equals(e.ErrorCode))
                    {
                        Chat.LogErrorMessage("Sorry, you entered your password incorrectly.  Please try again.");
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error during request for RDP Session : " + ex.Message, ex);
                Chat.LogErrorMessage("An error occured trying to send " + _engagement.SecondParty.Person.Firstname + " a request to control their desktop.");
            }
        }

        private void ElevatedRequestRdpSession()
        {
            string tokenId;
            string securityKey;
            if (
                BlitsMeClientAppContext.CurrentAppContext.Elevate(
                    "This connection requires you to verify your identity, please enter your BlitsMe password to connect to " +
                    _engagement.SecondParty.Person.Name + ".", out tokenId, out securityKey))
            {
                RequestRdpSession(tokenId, securityKey);
            }
            else
            {
                Chat.LogErrorMessage("Failed to elevate privileges to connect to " + _engagement.SecondParty.Person.Name);
                throw new Exception("Failed to gain unattended access through elevation");
            }
        }

        private bool CheckRdpActive()
        {
            if (_bmssHandle != null && !_bmssHandle.HasExited && IsWindow(_bmssHandle.MainWindowHandle) &&
                _bmssHandle.MainWindowTitle.Contains("BlitsMe"))
            {
                // hey, we have a valid window, raise it and bring it to the front
                // First call SwitchToThisWindow to unminimize it if it is minimized
                SwitchToThisWindow(_bmssHandle.MainWindowHandle, true);
                // Then foreground it so that it is at the front.
                SetForegroundWindow(_bmssHandle.MainWindowHandle);
                // we are done - window acticated nothing more to do so bug out
                return true;
            }
            // if we get this far then _bmssHandle is not valid so null it to be sure.
            _bmssHandle = null;
            return false;
        }

        /*
        // This is called once our async request to second party to control his desktop has completed
        private void ProcessRequestRDPSessionResponse(RDPRequestRq request, RDPRequestRs response, Exception e, IChatMessage chatElement)
        {

            if (e != null)
            {
                if (e is MessageException<RDPRequestRs>)
                {
                    var exception = (MessageException<RDPRequestRs>)e;
                    if ("WILL_NOT_PROCESS_ELEVATE".Equals(exception.ErrorCode))
                    {
                        // need elevation
                        string tokenId;
                        string securityKey;
                        if (BlitsMeClientAppContext.CurrentAppContext.Elevate(
                            "This connection requires you to verify your identify, please enter your BlitsMe password to connect to " +
                            _engagement.SecondParty.Person.Name + ".",
                            out tokenId, out securityKey))
                        {
                            RequestRdpSession(tokenId, securityKey);
                        }
                        else
                        {
                            Chat.LogErrorMessage("Failed to elevate privileges to connect to " +
                                                 _engagement.SecondParty.Person.Name);
                        }
                    }
                }
                else
                {
                    // The message wasn't delivered
                    IsActive = false;
                    Logger.Error("Received a async response to " + request.id + " that is an error", e);
                }
            }
            else
            {
                // The message was delivered
                IsActive = true;
                // Raise an activity that we managed to send a rdp request to second party successfully.
                OnNewActivity(new RemoteDesktopActivity(_engagement, RemoteDesktopActivity.REMOTE_DESKTOP_REQUEST) { From = "_SELF", To = _engagement.SecondParty.Person.Username });
            }
        }
         */

        // this is called by the request manager when we receive a answer our remote desktop request (yes or no)
        internal void ProcessRemoteDesktopRequestResponse(RDPRequestResponseRq request)
        {
            // Hey, we received an answer, note the activity
            OnNewActivity(new RemoteDesktopActivity(_engagement, RemoteDesktopActivity.REMOTE_DESKTOP_RESPONSE) { To = "_SELF", From = _engagement.SecondParty.Person.Username, Answer = request.accepted });
            if (request.accepted)
            {
                // ok, he wants us to control his desktop
                IsActive = true;
                // note that we are go to remote control second partys desktop
                //_engagement.IsRemoteControlActive = true;
                // print message in chat that we are about to go ahead and connect
                Chat.LogSecondPartySystemMessage(_engagement.SecondParty.Person.Firstname + " accepted your remote assistance request, please wait while we establish a connection...");
                try
                {
                    int port = Client.Start(request.connectionId);
                    String viewerExe = System.IO.Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]) + "\\bmss.exe";
                    var parameters = "-username=\"" + _engagement.SecondParty.Person.Name + "\" -copyrect=yes -encoding=tight -compressionlevel=9 -jpegimagequality=3 -scale=auto -host=127.0.0.1 -port=" + port;
                    Logger.Debug("Running " + viewerExe + " " + parameters);
                    _bmssHandle = Process.Start(viewerExe, parameters);

                }
                catch (Exception e)
                {
                    Chat.LogErrorMessage("Failed to create a connection to " + _engagement.SecondParty.Person.Username);
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
            IsUnderway = true;
            Chat.LogSystemMessage("Launching BlitsMe Support Screen...");
            Logger.Info("RDP client has connected to the proxy to " + _engagement.SecondParty.Person.Username + ".");
            OnNewActivity(new RemoteDesktopActivity(_engagement, RemoteDesktopActivity.REMOTE_DESKTOP_CONNECT) { From = "_SELF", To = _engagement.SecondParty.Person.Username });
        }

        private void ClientOnConnectionClosed(object sender, EventArgs eventArgs)
        {
            IsUnderway = false;
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
