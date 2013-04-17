using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Threading;
using BlitsMe.Agent.Components.Functions.API;
using BlitsMe.Agent.Managers;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using BlitsMe.Communication.P2P.RUDP.Tunnel;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;
using BlitsMe.Communication.P2P.RUDP.Utils;
using log4net;
using Function = BlitsMe.Agent.Components.Functions.FileSend.Function;

namespace BlitsMe.Agent.Components
{

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

        public String SecondPartyUsername { get { return SecondParty == null ? null : SecondParty.Username; } }

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

        private readonly Dictionary<String, IFunction> _functions;

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
            // Setup the functions of this engagement 
            _functions = new Dictionary<string, IFunction>();
            _functions.Add("FileSend", new Function(_appContext, this));
            _functions.Add("RemoteDesktop", new Functions.RemoteDesktop.Function(_appContext, this));
        }

        public bool hasFunction(String function)
        {
            return _functions.ContainsKey(function);
        }

        public IFunction getFunction(String function)
        {
            if (hasFunction(function))
            {
                return _functions[function];
            }
            throw new Exception("Function " + function + " not supported.");
        }

        #region Tunneling Functionality

        // The transportManager itself
        private readonly TransportManager _transportManager;
        internal TransportManager TransportManager { get { return _transportManager; } }

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
                var response = _appContext.ConnectionManager.Connection.Request<InitP2PConnectionRq,InitP2PConnectionRs>(initRq);
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
                    Logger.Info("Successfully completed incoming sync with " + SecondParty.Username + "-" + SecondParty.ShortCode);
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

        public void Close()
        {
            Chat.Close();
        }
    }

}
