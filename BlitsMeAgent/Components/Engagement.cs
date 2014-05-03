using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading;
using BlitsMe.Agent.Components.Functions.API;
using BlitsMe.Agent.Components.Functions.Chat;
using BlitsMe.Agent.Components.Person;
using BlitsMe.Agent.Managers;
using BlitsMe.Cloud.Messaging.API;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using BlitsMe.Communication.P2P.P2P.Tunnel;
using BlitsMe.Communication.P2P.RUDP.Tunnel;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;
using BlitsMe.Communication.P2P.RUDP.Utils;
using log4net;
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
        private Attendance _secondParty;
        internal Attendance SecondParty
        {
            get { return _secondParty; }
            set
            {
                _secondParty = value;
                _secondParty.PropertyChanged += SecondPartyPropertyChanged;
            }
        }

        public object TunnelWaitLock = new object();

        private bool _isRemoteControlActive = false;
        internal bool IsRemoteControlActive
        {
            get
            {
                return _isRemoteControlActive;
            }
            set
            {
                if ((_isRemoteControlActive != value) && (_isRemoteControlActive == null || !_isRemoteControlActive.Equals(value)))
                {
                    _isRemoteControlActive = value;
                    OnPropertyChanged("IsRemoteControlActive");
                }
            }
        }
        
        //internal string IsRemoteControlActivestring
        //{
        //    get
        //    {
        //        return _isRemoteControlActive.ToString();
        //    }
        //}

        internal readonly Dictionary<String, IFunction> Functions;

        public Engagement(BlitsMeClientAppContext appContext, Attendance personAttendance)
        {
            this._appContext = appContext;
            Interactions = new Interactions(this);
            // This is to pickup logouts/connection disconnections
            _appContext.LoginManager.LoggedOut += LogoutOccurred;
            SecondParty = personAttendance;
            _countdownToDeactivation = new Timer(TimeoutToTunnelClose) { AutoReset = false };
            _countdownToDeactivation.Elapsed += (sender, args) => CompleteDeactivation();
            //_transportManager = new TransportManager();
            // Setup the functions of this engagement 
            Functions = new Dictionary<string, IFunction>
                {
                    {"Chat", new Functions.Chat.Function(_appContext,this)},
                    {"FileSend", new  Functions.FileSend.Function(_appContext, this)},
                    {"RemoteDesktop", new Functions.RemoteDesktop.Function(_appContext, this)},
                };
            Functions.Values.ToList().ForEach(function =>
            {
                function.Activate += (sender, args) => { Active = true; };
                function.Deactivate += (sender, args) => SuggestCountdownToDeactivation();
            });
        }


        public String SecondPartyUsername { get { return SecondParty == null ? null : SecondParty.Person.Username; } }

        // This is to made sure the tunnel updates if the shortcode change (in other words we start talking with a different resource)
        private void SecondPartyPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("ActiveShortCode"))
            {
                //DisconnectTunnels();
                //SetupOutgoingTunnel();
            }
        }

        // this is so people can listen to us
        public event PropertyChangedEventHandler PropertyChanged;

        void LogoutOccurred(object sender, LoginEventArgs e)
        {
            // Disconnect Tunnels
            //DisconnectTunnels();
        }

        public void OnPropertyChanged(String property)
        {
            if (_appContext.IsShuttingDown) return;
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(property));
        }

        #region State Management

#if DEBUG
        // 2 minutes timeout
        private const double TimeoutToTunnelClose = 120000;
#else
        // 30 minutes timeout
        private const double TimeoutToTunnelClose = 1800000;
#endif
        private readonly Timer _countdownToDeactivation;

        internal Interactions Interactions;

        internal bool Active
        {
            get { return _active; }
            private set
            {
                if (_active != value)
                {
                    _active = value;
                    Logger.Debug("Engagement with " + SecondParty.Person.Username + " has become " + (_active ? "active" : "inactive"));
                    if (_active)
                    {
                        //SetupOutgoingTunnel();
                        OnActivate(EventArgs.Empty);
                    }
                    else
                    {
                        Logger.Debug("Shutting down outgoing tunnel to " + SecondParty.Person.Username + ", deactivating engagement.");
                        //DisconnectOutgoingTunnel();
                        OnDeactivate(EventArgs.Empty);
                    }
                    OnPropertyChanged("Active");
                }
            }
        }

        // Is this engagement considered Active
        public event EventHandler Activate;

        public void OnActivate(EventArgs e)
        {
            if (_appContext.IsShuttingDown) return;
            EventHandler handler = Activate;
            if (handler != null) handler(this, e);
        }

        public event EventHandler Deactivate;

        public void OnDeactivate(EventArgs e)
        {
            if (_appContext.IsShuttingDown) return;
            EventHandler handler = Deactivate;
            if (handler != null) handler(this, e);
        }


        internal bool IsChildrenActive
        {
            get { return Functions.Values.Any(function => function.IsActive); }
        }

        private void SuggestCountdownToDeactivation()
        {
            if (Active)
            {
                Logger.Debug("Starting countdown to deactivation in " + TimeoutToTunnelClose / 60000 + " mins");
                _countdownToDeactivation.Stop();
                _countdownToDeactivation.Start();
            }
        }

        private bool _active;
        private bool _closing;
        private bool _isUnread;

        public bool IsUnread
        {
            get { return _isUnread; }
            set
            {
                if (_isUnread != value)
                {
                    _isUnread = value;
                    //Logger.Debug(SecondParty.Person.Username + " is now " + (_isUnread ? "unread" : "read"));
                    OnPropertyChanged("IsUnread");
                }
            }
        }

        private void CompleteDeactivation()
        {
            if (!IsChildrenActive)
            {
                Active = false;
            }
            else
            {
                Logger.Warn("Cannot deativate, children are still busy, resetting timer");
                SuggestCountdownToDeactivation();
            }
        }

        #endregion

        #region Tunneling Functionality
/*
        // The thread used for creating the endPointManager
        private Thread _p2PConnectionCreatorThread;

        // The transportManager itself
        private readonly ITransportManager _transportManager;
        internal ITransportManager TransportManager { get { return _transportManager; } }

        private IUDPTunnel _outgoingTunnel;
        internal IUDPTunnel OutgoingTunnel
        {
            get { return _outgoingTunnel; }
            set
            {
                _outgoingTunnel = value;
                _outgoingTunnel.Disconnected += OutgoingTunnelOnDisconnected;
                _outgoingTunnel.Connected += OutgoingTunnelOnConnected;
                OnPropertyChanged("OutgoingTunnel");
            }
        }

        public bool WaitTunnel(int timeout = 30000)
        {
            lock (TunnelWaitLock)
            {
                Logger.Debug("Testing is tunnel to " + SecondParty.Person.Username + " is active");
                while (!IsTunnelActive)
                {
                    Logger.Debug("Tunnel to " + SecondParty.Person.Username + " is not active, waiting");
                    if (!Monitor.Wait(TunnelWaitLock, timeout))
                    {
                        Logger.Error("Failed to create a tunnel between " + _appContext.CurrentUserManager.CurrentUser.Name + "(me) and " + SecondParty.Person.Username + " : timed out waiting for tunnel");
                        return false;
                    }
                }
            }
            return true;
        }

        private void OutgoingTunnelOnConnected(object sender, EventArgs args)
        {
            _transportManager.AddTunnel(_outgoingTunnel,
                                        (_incomingTunnel == null || !_incomingTunnel.IsTunnelEstablished) ? 1 : 2);
            OnPropertyChanged("OutgoingTunnel");
            lock (TunnelWaitLock)
            {
                Monitor.PulseAll(TunnelWaitLock);
            }
        }

        private void OutgoingTunnelOnDisconnected(object sender, EventArgs args)
        {
            OnPropertyChanged("OutgoingTunnel");
            lock (TunnelWaitLock)
            {
                Monitor.PulseAll(TunnelWaitLock);
            }
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
                OnPropertyChanged("IncomingTunnel");
            }
        }

        public bool IsTunnelActive
        {
            get { return TransportManager.IsActive; }
        }

        private void IncomingTunnelOnConnected(object sender, EventArgs args)
        {
            _transportManager.AddTunnel(_incomingTunnel, (_outgoingTunnel == null || !_outgoingTunnel.IsTunnelEstablished) ? 1 : 2);
            OnPropertyChanged("IncomingTunnel");
            lock (TunnelWaitLock)
            {
                Monitor.PulseAll(TunnelWaitLock);
            }
        }

        private void IncomingTunnelOnDisconnected(object sender, EventArgs args)
        {
            OnPropertyChanged("IncomingTunnel");
            lock (TunnelWaitLock)
            {
                Monitor.PulseAll(TunnelWaitLock);
            }
        }

        private void SetupOutgoingTunnel()
        {
            if (!_closing && (SecondParty.ActiveShortCode != null) && Active)
            {
                if ((_p2PConnectionCreatorThread == null || !_p2PConnectionCreatorThread.IsAlive)
                    && (_outgoingTunnel == null || !_outgoingTunnel.IsTunnelEstablished))
                {
#if DEBUG
                    Logger.Debug("Starting up thread to setup outgoing tunnel to " + SecondParty.Person.Username);
#endif

                    _p2PConnectionCreatorThread = new Thread(InitP2PConnection)
                        {
                            IsBackground = true,
                            Name = "_p2pConnectionCreator[" + SecondParty.Person.Username + "-" + SecondParty.ActiveShortCode + "-outgoing" + "]"
                        };
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
            var initRq = new InitP2PConnectionRq { shortCode = SecondParty.ActiveShortCode };
            try
            {
                var response = _appContext.ConnectionManager.Connection.Request<InitP2PConnectionRq, InitP2PConnectionRs>(initRq);
#if DEBUG
                Logger.Debug("Got response from p2p connection request");
#endif
                try
                {
                    OutgoingTunnel = _appContext.P2PManager.CompleteTunnel(response.uniqueId);
                    OutgoingTunnel.Id = SecondParty.Person.Username + "-" + SecondParty.ActiveShortCode + "-outgoing";
#if DEBUG
                    Logger.Debug("Got endPointManager from p2p manager");
#endif
                    var peer = new PeerInfo()
                        {
                            ExternalEndPoint = response.externalEndPoint != null ? new IPEndPoint(IPAddress.Parse(response.externalEndPoint.address),
                                                              Convert.ToInt32(response.externalEndPoint.port)) : null,
                        };
                    foreach (var ipEndPointElement in response.internalEndPoints)
                    {
                        peer.InternalEndPoints.Add(new IPEndPoint(IPAddress.Parse(ipEndPointElement.address), ipEndPointElement.port));
                    }
                    OutgoingTunnel.SyncWithPeer(peer, 10000);
                    OnPropertyChanged("OutgoingTunnel");
                    Logger.Info("Successfully completed outgoing sync with " + SecondParty.Person.Username + "-" + SecondParty.ActiveShortCode);
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
                TransportManager.CloseTunnel(_outgoingTunnel);
                _outgoingTunnel = null;
                OnPropertyChanged("OutgoingTunnel");
            }
        }

        private void DisconnectIncomingTunnel()
        {
            if (_incomingTunnel != null)
            {
                TransportManager.CloseTunnel(_incomingTunnel);
                _incomingTunnel = null;
                OnPropertyChanged("IncomingTunnel");
            }
        }
        */
        #endregion


        public bool HasFunction(String function)
        {
            return Functions.ContainsKey(function);
        }

        public IFunction GetFunction(String function)
        {
            if (HasFunction(function))
            {
                return Functions[function];
            }
            throw new Exception("Function " + function + " not supported.");
        }

        public void Close()
        {
            _closing = true;
            Interactions.Close();
            Functions.Values.ToList().ForEach(function => function.Close());
            //DisconnectTunnels();
        }

        public void SetRating(string interactionId, string ratingName, int rating)
        {
            var request = new RateRq {username = SecondParty.Person.Username, interactionId = interactionId, ratingName = ratingName, rating = rating};
            try
            {
                _appContext.ConnectionManager.Connection.RequestAsync<RateRq, RateRs>(request, ProcessRateResponse);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to send rate rq " + request + " : " + e.Message,e);
            }
        }

        private void ProcessRateResponse(RateRq request, Response response, Exception e)
        {
            if (e != null)
            {
                Logger.Error("Failed to send the rating request : " + e.Message, e);
            }
            else
            {
                Logger.Debug("Successfully rated " + SecondParty.Person.Username + " " + request.rating + " for " +
                             request.ratingName + " for interaction " + request.interactionId);
            }
        }

        public void ActivityOccured(EngagementActivity args)
        {
            IsUnread = true;
            //SetupOutgoingTunnel();
            Interactions.CurrentOrNewInteraction.RecordActivity(args);
        }
    }

}
