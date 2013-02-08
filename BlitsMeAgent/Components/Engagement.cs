using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Threading;
using BlitsMe.Agent.Components.Chat;
using BlitsMe.Agent.Components.Notification;
using BlitsMe.Agent.Components.RDP;
using BlitsMe.Agent.Managers;
using BlitsMe.Cloud.Exceptions;
using BlitsMe.Cloud.Messaging.API;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using BlitsMe.Communication.P2P.RUDP.Connector.API;
using BlitsMe.Communication.P2P.RUDP.Tunnel;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;
using BlitsMe.Communication.P2P.RUDP.Utils;
using log4net;

namespace BlitsMe.Agent.Components
{
    internal delegate void RDPSessionRequestResponseEvent(object sender, RDPSessionRequestResponseArgs args);

    internal delegate void RDPIncomingRequestEvent(object sender, RDPIncomingRequestArgs args);

    /* This class is all about interaction with a user, their chat, their billing, their everything */
    internal class Engagement : INotifyPropertyChanged
    {
        // Our app context
        private readonly BlitsMeClientAppContext _appContext;
        // Our logger
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Engagement));
        // The person we are engaging with
        private Person.Person _secondParty;
        internal Person.Person SecondParty
        {
            get { return _secondParty; }
            set
            {
                _secondParty = value;
                _secondParty.PropertyChanged += SecondPartyPropertyChanged;
            }
        }

        private void SecondPartyPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("ShortCode"))
            {
                // Get the endPointManager going
                if (OutgoingTunnel != null && OutgoingTunnel.IsTunnelEstablished)
                {
                    OutgoingTunnel.Close();
                    OnPropertyChanged(new PropertyChangedEventArgs("OutgoingTunnel"));
                }
                if (_secondParty.ShortCode != null)
                {
                    SetupOutgoingTunnel();
                }
            }
        }

        // The username of the person we are engaging with
        public string SecondPartyUsername { get { return SecondParty.Username; } }
        // The thread used for creating the endPointManager
        private Thread _p2PConnectionCreatorThread;
        // The chat part of the engagement
        private Chat.Chat _chat;

        // this is so people can listen to us
        public event PropertyChangedEventHandler PropertyChanged;

        void LogoutOccurred(object sender, LoginEventArgs e)
        {
            // Disconnect Tunnels
            DisconnectTunnels();
        }

        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, e);
        }

        public Chat.Chat Chat
        {
            get { return _chat ?? (_chat = new Chat.Chat(_appContext, this)); }
            set { _chat = value; }
        }

        public Engagement(BlitsMeClientAppContext appContext, Person.Person person)
        {
            this._appContext = appContext;
            // This is to pickup logouts/connection disconnections
            _appContext.LoginManager.LoggedOut += LogoutOccurred;
            SecondParty = person;
            _transportManager = new TransportManager();
            // Kickoff a tunnel if we have a shortcode
            if (SecondParty.ShortCode != null)
            {
                SetupOutgoingTunnel();
            }
        }

        #region Tunneling Functionality

        // The transportManager itself
        private readonly TransportManager _transportManager;

        private IUDPTunnel _outgoingTunnel;
        internal IUDPTunnel OutgoingTunnel
        {
            get { return _outgoingTunnel; }
            set
            {
                _outgoingTunnel = value;
                _outgoingTunnel.Disconnected += OutgoingTunnelOnDisconnected;
                _outgoingTunnel.Connected += OutgoingTunnelOnConnected;
                OnPropertyChanged(new PropertyChangedEventArgs("OutgoingTunnel"));
            }
        }

        private void OutgoingTunnelOnConnected(object sender, EventArgs args)
        {
            _transportManager.AddTunnel(_outgoingTunnel, (_incomingTunnel == null || !_incomingTunnel.IsTunnelEstablished) ? 1 : 2);
            OnPropertyChanged(new PropertyChangedEventArgs("OutgoingTunnel"));
        }

        private void OutgoingTunnelOnDisconnected(object sender, EventArgs args)
        {
            // Attempt once to get it going again
            if (!_appContext.isShuttingDown)
            {
                SetupOutgoingTunnel();
            }
            OnPropertyChanged(new PropertyChangedEventArgs("OutgoingTunnel"));
        }


        private IUDPTunnel _incomingTunnel;
        internal IUDPTunnel IncomingTunnel
        {
            get { return _incomingTunnel; }
            set
            {
                _incomingTunnel = value;
                _incomingTunnel.Disconnected += IncomingTunnelOnDisconnected;
                _incomingTunnel.Connected += IncomingTunnelOnConnected;
                OnPropertyChanged(new PropertyChangedEventArgs("IncomingTunnel"));
            }
        }

        private void IncomingTunnelOnConnected(object sender, EventArgs args)
        {
            _transportManager.AddTunnel(_incomingTunnel, (_outgoingTunnel == null || !_outgoingTunnel.IsTunnelEstablished) ? 1 : 2);
            OnPropertyChanged(new PropertyChangedEventArgs("IncomingTunnel"));
        }

        private void IncomingTunnelOnDisconnected(object sender, EventArgs args)
        {
            OnPropertyChanged(new PropertyChangedEventArgs("IncomingTunnel"));
        }

        private void SetupOutgoingTunnel()
        {
            if (_p2PConnectionCreatorThread == null || !_p2PConnectionCreatorThread.IsAlive)
            {
#if DEBUG
                Logger.Debug("Starting up thread to setup p2p link");
#endif

                _p2PConnectionCreatorThread = new Thread(InitP2PConnection) { IsBackground = true };
                _p2PConnectionCreatorThread.Name = "_p2pConnectionCreatorThread[" +
                                                   _p2PConnectionCreatorThread.ManagedThreadId + "]";
                _p2PConnectionCreatorThread.Start();
            }
            else
            {
#if DEBUG
                Logger.Debug("Will not start P2P Connection, one is currently underway");
#endif
            }
        }

        private void InitP2PConnection()
        {
            var initRq = new InitP2PConnectionRq { shortCode = SecondParty.ShortCode };
            try
            {
                var response = (InitP2PConnectionRs)_appContext.ConnectionManager.Connection.Request(initRq);
#if DEBUG
                Logger.Debug("Got response from p2p connection request");
#endif
                try
                {
                    OutgoingTunnel = _appContext.P2PManager.CompleteTunnel(response.uniqueId);
                    OutgoingTunnel.id = "outgoing";
#if DEBUG
                    Logger.Debug("Got endPointManager from p2p manager");
#endif
                    OutgoingTunnel.SyncWithPeer(new PeerInfo(
                                    new IPEndPoint(IPAddress.Parse(response.internalEndpointIp),
                                                   Convert.ToInt32(response.internalEndpointPort)),
                                    new IPEndPoint(IPAddress.Parse(response.externalEndpointIp),
                                                   Convert.ToInt32(response.externalEndpointPort))
                                    ), 10000);
                    OnPropertyChanged(new PropertyChangedEventArgs("OutgoingTunnel"));
#if DEBUG
                    Logger.Debug("Synced with peer, connection setup");
#endif
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to sync with peer : " + e.Message, e);
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to setup P2P Connection : " + e.Message, e);
            }

        }

        public void SetupIncomingTunnel(IUDPTunnel awareIncomingTunnel, PeerInfo peerinfo)
        {
            IncomingTunnel = awareIncomingTunnel;
            IncomingTunnel.id = "incoming";
            var p2pListenerThread = new Thread(() => IncomingTunnelWaitSync(peerinfo)) { IsBackground = true };
            p2pListenerThread.Name = "p2pListenerThread[" + p2pListenerThread.ManagedThreadId + "]";
            p2pListenerThread.Start();
        }

        private void IncomingTunnelWaitSync(PeerInfo peerIP)
        {
            try
            {
                _incomingTunnel.WaitForSyncFromPeer(peerIP, 10000);
            }
            catch (Exception e)
            {
                Logger.Error("Failed waiting for sync from peer [" + peerIP + "] : " + e.Message, e);
            }
        }

        private void DisconnectTunnels()
        {
            if (_incomingTunnel != null && !_incomingTunnel.Closing)
            {
                _incomingTunnel.Close();
                _incomingTunnel = null;
            }
            if (_outgoingTunnel != null && !_outgoingTunnel.Closing)
            {
                _outgoingTunnel.Close();
                _outgoingTunnel = null;
            }
        }

        #endregion

        #region RDP Functionality

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
            this.SecondParty.ShortCode = shortCode;
            RDPNotification rdpNotification = new RDPNotification() { Message = SecondParty.Name + " would like to access your desktop.", From = SecondParty.Username };
            rdpNotification.ProcessDenyRDP += RDPNotificationOnProcessDenyRDP;
            rdpNotification.ProcessAcceptRDP += RDPNotificationOnProcessAcceptRDP;
            _appContext.NotificationManager.AddNotification(rdpNotification);
            _chat.LogSystemMessage(SecondParty.Name + " sent you a request to control your desktop.");
            OnRDPIncomingRequestEvent(new RDPIncomingRequestArgs(this));
        }

        private void RDPNotificationOnProcessAcceptRDP(object sender, EventArgs eventArgs)
        {
            _chat.LogSystemMessage("You accepted the desktop assistance request from " + SecondParty.Name);
            try
            {
                Server.Start();
            }
            catch (Exception e)
            {
                Logger.Error("Failed to start server : " + e.Message, e);
            }
            RDPRequestResponseRq request = new RDPRequestResponseRq() { accepted = true, shortCode = SecondParty.ShortCode };
            try
            {
                _appContext.ConnectionManager.Connection.Request(request);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to send a RDP acceptance request to " + SecondParty.Username);
            }
        }

        private void RDPNotificationOnProcessDenyRDP(object sender, EventArgs eventArgs)
        {
            _chat.LogSystemMessage("You denied the desktop assistance request from " + SecondParty.Name);
            RDPRequestResponseRq request = new RDPRequestResponseRq() { accepted = false, shortCode = SecondParty.ShortCode };
            try
            {
                _appContext.ConnectionManager.Connection.Request(request);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to send a RDP denial request to " + SecondParty.Username);
            }
        }

        private Client _client;
        private Client Client
        {
            get
            {
                if (_client == null)
                {
                    _client = new Client(_transportManager);
                }
                return _client;
            }
        }

        private Server _server;
        private Server Server
        {
            get
            {
                if (_server == null)
                {
                    _server = new Server(_transportManager);
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
            _chat.LogServiceCompleteMessage("You were just helped by " + SecondParty.Name + ", please rate his service below.");
        }

        internal void RequestRDPSession()
        {
            RDPRequestRq request = new RDPRequestRq() { shortCode = SecondParty.ShortCode };
            try
            {
                ChatElement chatElement = _chat.LogSystemMessage("You sent " + SecondParty.Name + " a request to control their desktop.");
                _appContext.ConnectionManager.Connection.RequestAsync(request, (req, res) => ProcessRequestRDPSessionResponse(req, res, chatElement));
            }
            catch (Exception ex)
            {
                Logger.Error("Error during request for RDP Session : " + ex.Message, ex);
                _chat.LogSystemMessage("An error occured trying to send " + SecondParty.Name + " a request to control their desktop.");
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

        public void ProcessRDPRequestResponse(string shortCode, bool accepted)
        {
            if (accepted)
            {
                _chat.LogSystemMessage(SecondParty.Name + " accepted your remote assistance request.");
                try
                {
                    int port = Client.Start();
                    Process.Start("c:\\Program Files\\TightVNC\\tvnviewer.exe", "127.0.0.1:" + port);
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to start RDP client : " + e.Message, e);
                }
            }
            else
            {
                _chat.LogSystemMessage(SecondParty.Name + " did not accept your remote assistance request.");
            }
        }

        #endregion

    }

}
