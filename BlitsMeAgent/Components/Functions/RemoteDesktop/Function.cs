using System;
using System.Diagnostics;
using Gwupe.Agent.Components.Functions.API;
using Gwupe.Agent.Components.Functions.Chat;
using Gwupe.Agent.Components.Functions.RemoteDesktop.ChatElement;
using Gwupe.Cloud.Exceptions;
using Gwupe.Cloud.Messaging.Request;
using Gwupe.Cloud.Messaging.Response;
using Gwupe.Common.Security;
using log4net;
using System.Runtime.InteropServices;



namespace Gwupe.Agent.Components.Functions.RemoteDesktop
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

        private readonly GwupeClientAppContext _appContext;
        private readonly Engagement _engagement;
        public override String Name { get { return "RemoteDesktop"; } }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(FileSend.Function));
        private Process _bmssHandle = null;

        private Chat.Function Chat { get { return _engagement.Functions["Chat"] as Chat.Function; } }

        internal Function(GwupeClientAppContext appContext, Engagement engagement)
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
                OnNewActivity(new RemoteDesktopActivity(_engagement, RemoteDesktopActivity.REMOTE_DESKTOP_REQUEST) { To = "_SELF", From = _engagement.SecondParty.Party.Username });
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
                message = _engagement.SecondParty.Party.Firstname +
                          " requested control of your desktop. Unattended access will be granted in 10 seconds.";
                chatElement = new RdpRequestUnattendedChatElement(10)
                {
                    Message = message,
                    SpeakTime = DateTime.Now,
                    UserName = _engagement.SecondParty.Party.Username,
                };

            }
            else
            {
                message = _engagement.SecondParty.Party.Firstname + " requested control of your desktop.";
                chatElement = new RdpRequestChatElement()
                {
                    Message = message,
                    SpeakTime = DateTime.Now,
                    UserName = _engagement.SecondParty.Party.Username,
                };

            }
            Chat.Conversation.AddMessage(chatElement);
            // Notify that there is activity in the chat
            OnNewActivity(new ChatActivity(_engagement, ChatActivity.LOG_RDP_REQUEST)
            {
                From = _engagement.SecondParty.Party.Username,
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
                Chat.LogSystemMessage("You accepted the desktop assistance request from " + _engagement.SecondParty.Party.Firstname + ", please wait while they connect to your desktop.  This window will go blue when they are connected.");
                try
                {
                    // this will restart the service if its offline
                    try
                    {
                        _appContext.GwupeServiceProxy.Ping();
                    }
                    catch
                    {
                        _appContext.RestartGwupeService();
                    }
                    // Startup the underlying VNC service
                    if (_appContext.GwupeServiceProxy.VNCStartService())
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
                SendRdpRequestResponse(true, connectionId, delegate (RDPRequestResponseRq rq, RDPRequestResponseRs rs, Exception exception) { IsActive = exception == null; });
            }
            else
            {
                // mark the remote control as not underway
                //_engagement.IsRemoteControlActive = false;
                // Log in the chat that we denied the request
                Chat.LogSystemMessage("You denied the desktop assistance request from " + _engagement.SecondParty.Party.Firstname);
                // notify the second party that he cannot connect.
                SendRdpRequestResponse(false, null, delegate (RDPRequestResponseRq rq, RDPRequestResponseRs rs, Exception arg3) { IsActive = false; });
            }
            OnNewActivity(new RemoteDesktopActivity(_engagement, RemoteDesktopActivity.REMOTE_DESKTOP_RESPONSE) { To = _engagement.SecondParty.Party.Username, From = "_SELF", Answer = accept });
        }

        // generic method to send a response to the remote desktop request
        private void SendRdpRequestResponse(bool answer, String connectionId, Action<RDPRequestResponseRq, RDPRequestResponseRs, Exception> handler)
        {
            // compile the request
            RDPRequestResponseRq request = new RDPRequestResponseRq()
            {
                accepted = answer,
                shortCode = _engagement.SecondParty.ActiveShortCode,
                username = _engagement.SecondParty.Party.Username,
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
                Logger.Error("Failed to send a RDP request (answer=" + answer + ") to " + _engagement.SecondParty.Party.Username, e);
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
            OnNewActivity(new RemoteDesktopActivity(_engagement, RemoteDesktopActivity.REMOTE_DESKTOP_CONNECT) { From = _engagement.SecondParty.Party.Username, To = "_SELF" });
        }

        // This event is fired when the connection to our server is closed
        private void ServerOnConnectionClosed(object sender, EventArgs eventArgs)
        {
            IsActive = false;
            IsUnderway = false;
            Logger.Info("Server connection closed, notifying end of service.");
            if (_engagement.SecondParty.Relationship.TheyHaveUnattendedAccess)
            {
                Chat.LogServiceCompleteMessage("You were just helped by " + _engagement.SecondParty.Party.Name, false);
            }
            else
            {
                Chat.LogServiceCompleteMessage("You were just helped by " + _engagement.SecondParty.Party.Name +
                                               ", please rate their service below.");
            }
            OnNewActivity(new RemoteDesktopActivity(_engagement, RemoteDesktopActivity.REMOTE_DESKTOP_DISCONNECT) { From = _engagement.SecondParty.Party.Username, To = "_SELF" });
        }

        internal void RequestRdpSession()
        {
            //BlitsMeClientAppContext.CurrentAppContext.UIManager.GetRemoteEngagement(_engagement);
            // all these checks are to make sure that we aren't currently accessing second partys desktop
            if (CheckRdpActive()) return;
            if (_engagement.SecondParty.Relationship.IHaveUnattendedAccess)
            {
                ElevatedRequestRdpSession();
            }
            else
            {
                RequestRdpSession(null);
            }
        }

        // Called When this user send a rdp request to the second party
        private void RequestRdpSession(ElevateToken token)
        {
            // now we compile the request to second party to control his desktop
            RDPRequestRq request = new RDPRequestRq()
            {
                shortCode = _engagement.SecondParty.ActiveShortCode,
                username = _engagement.SecondParty.Party.Username,
                interactionId = _engagement.Interactions.CurrentOrNewInteraction.Id,
                securityKey = token?.SecurityKey,
                tokenId = token?.TokenId,
            };
            try
            {
                // Actually send the message asynchronously
                //_appContext.ConnectionManager.Connection.RequestAsync<RDPRequestRq, RDPRequestRs>(request, (req, res, ex) => ProcessRequestRDPSessionResponse(req, res, ex, chatElement));
                try
                {
                    // if its unattended, indicate this
                    if (token != null)
                    {
                        Chat.LogSystemMessage("You have unattended access to their desktop, you will be granted access automatically after 10 seconds.");
                    }
                    else
                    {
                        // Print in chat that we sent the second party a rdp request
                        Chat.LogSystemMessage("You sent " + _engagement.SecondParty.Party.Firstname + " a request to control their desktop.");
                    }
                    var response = _appContext.ConnectionManager.Connection.Request<RDPRequestRq, RDPRequestRs>(request);
                    // The message was delivered
                    IsActive = true;
                    // Raise an activity that we managed to send a rdp request to second party successfully.
                    OnNewActivity(new RemoteDesktopActivity(_engagement, RemoteDesktopActivity.REMOTE_DESKTOP_REQUEST) { From = "_SELF", To = _engagement.SecondParty.Party.Username });
                }
                catch (MessageException<RDPRequestRs> e)
                {
                    if ("WILL_NOT_PROCESS_ELEVATE".Equals(e.ErrorCode) && (token == null))
                    {
                        // need elevation
                        ElevatedRequestRdpSession();
                    }
                    else if ("WILL_NOT_PROCESS_AUTH".Equals(e.ErrorCode))
                    {
                        Chat.LogErrorMessage("Sorry, you entered your password incorrectly.  Please try again.");
                    }
                    else if ("KEY_NOT_FOUND".Equals(e.ErrorCode))
                    {
                        // sometimes the user disappears and comes back with another shortcode, lets try that
                        if (_engagement.SecondParty.Presence.IsOnline &&
                            !_engagement.SecondParty.ActiveShortCode.Equals(_engagement.SecondParty.Presence.ShortCode))
                        {
                            Logger.Debug("ActiveShortCode is different from current presence shortcode, trying the new one");
                            _engagement.SecondParty.ActiveShortCode = _engagement.SecondParty.Presence.ShortCode;
                            RequestRdpSession(token);
                        }
                        else
                        {
                            throw;
                        }
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
                Chat.LogErrorMessage("An error occured trying to send " + _engagement.SecondParty.Party.Firstname + " a request to control their desktop.");
            }
        }

        private void ElevatedRequestRdpSession()
        {
            try
            {
                ElevateToken token = GwupeClientAppContext.CurrentAppContext.Elevate(
                    "This connection requires you to verify your identity, please enter your Gwupe password to connect to " +
                    _engagement.SecondParty.Party.Name + ".");
                RequestRdpSession(token);
            }
            catch (Exception ex)
            {
                Chat.LogErrorMessage("Failed to elevate privileges to connect to " + _engagement.SecondParty.Party.Name);
                throw new Exception("Failed to gain unattended access through elevation");
            }
        }

        private bool CheckRdpActive()
        {
            if (_bmssHandle != null && !_bmssHandle.HasExited && IsWindow(_bmssHandle.MainWindowHandle) &&
                _bmssHandle.MainWindowTitle.Contains("Gwupe"))
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
            OnNewActivity(new RemoteDesktopActivity(_engagement, RemoteDesktopActivity.REMOTE_DESKTOP_RESPONSE) { To = "_SELF", From = _engagement.SecondParty.Party.Username, Answer = request.accepted });
            if (request.accepted)
            {
                // ok, he wants us to control his desktop
                IsActive = true;
                // note that we are go to remote control second partys desktop
                //_engagement.IsRemoteControlActive = true;
                // print message in chat that we are about to go ahead and connect
                Chat.LogSecondPartySystemMessage(_engagement.SecondParty.Party.Firstname + " accepted your remote assistance request, please wait while we establish a connection...");
                try
                {
                    int port = Client.Start(request.connectionId);
                    String viewerExe = System.IO.Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]) + "\\gwupess.exe";
                    var parameters = "-username=\"" + _engagement.SecondParty.Party.Name + "\" -copyrect=yes -encoding=tight -compressionlevel=9 -jpegimagequality=3 -scale=auto -host=localhost -port=" + port;
                    Logger.Debug("Running " + viewerExe + " " + parameters);
                    _bmssHandle = Process.Start(viewerExe, parameters);

                }
                catch (Exception e)
                {
                    Chat.LogErrorMessage("Failed to create a connection to " + _engagement.SecondParty.Party.Username);
                    IsActive = false;
                    Logger.Error("Failed to start RDP client to " + _engagement.SecondParty.Party.Username + " : " + e.Message, e);
                    throw e;
                }
            }
            else
            {
                IsActive = false;
                Chat.LogSecondPartySystemMessage(_engagement.SecondParty.Party.Firstname + " did not accept your remote assistance request.");
            }
        }

        private void ClientOnConnectionAccepted(object sender, EventArgs eventArgs)
        {
            IsUnderway = true;
            Chat.LogSystemMessage("Launching Gwupe Support Screen...");
            Logger.Info("RDP client has connected to the proxy to " + _engagement.SecondParty.Party.Username + ".");
            OnNewActivity(new RemoteDesktopActivity(_engagement, RemoteDesktopActivity.REMOTE_DESKTOP_CONNECT) { From = "_SELF", To = _engagement.SecondParty.Party.Username });
        }

        private void ClientOnConnectionClosed(object sender, EventArgs eventArgs)
        {
            IsUnderway = false;
            Chat.LogSystemMessage("You disconnected from " + _engagement.SecondParty.Party.Firstname + "'s desktop.");
            Logger.Info("RDP client has disconnected from the proxy to " + _engagement.SecondParty.Party.Username + ".");
            IsActive = false;
            OnNewActivity(new RemoteDesktopActivity(_engagement, RemoteDesktopActivity.REMOTE_DESKTOP_DISCONNECT) { From = "_SELF", To = _engagement.SecondParty.Party.Username });
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
