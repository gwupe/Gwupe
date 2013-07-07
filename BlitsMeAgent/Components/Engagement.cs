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
using Timer = System.Timers.Timer;

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
#if DEBUG
        // 2 minutes timeout
        private const double TimeoutToTunnelClose = 120000;
#else
        // 30 minutes timeout
        private const double TimeoutToTunnelClose = 1800000;
#endif
        private readonly Timer _countdownToDeactivation;
        internal bool Active
        {
            get { return _active; }
            private set
            {
                if (_active != value)
                {
                    _active = value;
                    Logger.Debug("Engagement with " + SecondParty.Username + " has become " + (_active ? "active" : "inactive"));
                    if (_active)
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

        internal bool IsChildrenActive
        {
            get
            {
                return _chat.IsActive || _functions["FileSend"].IsActive || _functions["RemoteDesktop"].IsActive;
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
                    // Another one will start straight away because of the tunnel disconnected event
                    OutgoingTunnel.Close();
                    OnPropertyChanged(new PropertyChangedEventArgs("OutgoingTunnel"));
                } else
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
            get
            {
                if (_chat == null)
                {
                    _chat = new Chat.Chat(_appContext, this);
                    // if we receive or send a message, we should get the tunnel going and also setup a countdown to deactive
                    _chat.NewMessage += (sender, args) =>
                    {
                        Active = true;
                        SuggestCountdownToDeactivation();
                    };
                }
                return _chat;
            }
            set { _chat = value; }
        }

        private void SuggestCountdownToDeactivation()
        {
            if(Active)
            {
                Logger.Debug("Starting countdown to deactivation in " + TimeoutToTunnelClose/60000 + " mins");
                _countdownToDeactivation.Stop();
                _countdownToDeactivation.Start();
            }
        }

        private readonly Dictionary<String, IFunction> _functions;

        public Engagement(BlitsMeClientAppContext appContext, Person.Person person)
        {
            this._appContext = appContext;
            // This is to pickup logouts/connection disconnections
            _appContext.LoginManager.LoggedOut += LogoutOccurred;
            SecondParty = person;
            _countdownToDeactivation = new Timer(TimeoutToTunnelClose) {AutoReset = false};
            _countdownToDeactivation.Elapsed += (sender, args) => CompleteDeactivation();
            Activate += OnActivate;
            Deactivate += OnDeactivate;
            _transportManager = new TransportManager();
            // Setup the functions of this engagement 
            _functions = new Dictionary<string, IFunction>
                {
                    {"FileSend", new Function(_appContext, this)},
                    {"RemoteDesktop", new Functions.RemoteDesktop.Function(_appContext, this)}
                };
            foreach (var function in _functions)
            {
                function.Value.Activate += (sender, args) => { Active = true; };
                function.Value.Deactivate += (sender, args) => SuggestCountdownToDeactivation();
            }
        }

        private void OnDeactivate(object sender, EventArgs eventArgs)
        {
            Logger.Debug("Shutting down outgoing tunnel to " + SecondParty.Username + ", deactivating engagement.");
            DisconnectOutgoingTunnel();
        }

        private void OnActivate(object sender, EventArgs eventArgs)
        {
            SetupOutgoingTunnel();
        }

        private void CompleteDeactivation()
        {
            if (!IsChildrenActive)
            {
                Active = false;
            } else
            {
                Logger.Warn("Cannot deativate, children are still busy, resetting timer");
                SuggestCountdownToDeactivation();
            }
        }

        public bool HasFunction(String function)
        {
            return _functions.ContainsKey(function);
        }

        public IFunction GetFunction(String function)
        {
            if (HasFunction(function))
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
            if (!_closing && Active)
            {
                SetupOutgoingTunnel();
            }
            OnPropertyChanged(new PropertyChangedEventArgs("OutgoingTunnel"));
        }


        private IUDPTunnel _incomingTunnel;
        private bool _active;
        private bool _closing;


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
            if (!_closing && (SecondParty.ShortCode != null) && Active)
            {
                if ((_p2PConnectionCreatorThread == null || !_p2PConnectionCreatorThread.IsAlive)
                    && (_outgoingTunnel == null || !_outgoingTunnel.IsTunnelEstablished))
                {
#if DEBUG
                    Logger.Debug("Starting up thread to setup outgoing tunnel to " + SecondParty.Username);
#endif

                    _p2PConnectionCreatorThread = new Thread(InitP2PConnection) {IsBackground = true};
                    _p2PConnectionCreatorThread.Name = "_p2pConnectionCreatorThread[" +
                                                       _p2PConnectionCreatorThread.ManagedThreadId + "]";
                    _p2PConnectionCreatorThread.Start();
                }
                else
                {
#if DEBUG
                    Logger.Debug("Will not start outgoing tunnel, one is currently underway");
#endif
                }
            }
        }


        private void InitP2PConnection()
        {
            var initRq = new InitP2PConnectionRq { shortCode = SecondParty.ShortCode };
            try
            {
                var response = _appContext.ConnectionManager.Connection.Request<InitP2PConnectionRq, InitP2PConnectionRs>(initRq);
#if DEBUG
                Logger.Debug("Got response from p2p connection request");
#endif
                try
                {
                    OutgoingTunnel = _appContext.P2PManager.CompleteTunnel(response.uniqueId);
                    OutgoingTunnel.Id = SecondParty.Username + "-" + SecondParty.ShortCode + "-outgoing";
#if DEBUG
                    Logger.Debug("Got endPointManager from p2p manager");
#endif
                    var peer = new PeerInfo()
                        {
                            ExternalEndPoint = new IPEndPoint(IPAddress.Parse(response.externalEndpointIp),
                                                              Convert.ToInt32(response.externalEndpointPort))
                        };
                    foreach (var ipEndPointElement in response.internalEndPoints)
                    {
                        peer.InternalEndPoints.Add(new IPEndPoint(IPAddress.Parse(ipEndPointElement.address), ipEndPointElement.port));
                    }
                    OutgoingTunnel.SyncWithPeer(peer, 10000);
                    OnPropertyChanged(new PropertyChangedEventArgs("OutgoingTunnel"));
                    Logger.Info("Successfully completed outgoing sync with " + SecondParty.Username + "-" + SecondParty.ShortCode);
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
            DisconnectIncomingTunnel();
            DisconnectOutgoingTunnel();
        }

        private void DisconnectOutgoingTunnel()
        {
            if (_outgoingTunnel != null)
            {
                _outgoingTunnel.Close();
                _outgoingTunnel = null;
            }
        }

        private void DisconnectIncomingTunnel()
        {
            if (_incomingTunnel != null)
            {
                _incomingTunnel.Close();
                _incomingTunnel = null;
            }
        }

        #endregion

        public void Close()
        {
            _closing = true;
            DisconnectTunnels();
            Chat.Close();
        }
    }

}
