﻿using System;
using System.Net;
using System.Threading;
using Gwupe.Agent.Components.Functions.API;
using Gwupe.Agent.Components.Person;
using Gwupe.Communication.P2P.P2P.Connector;
using Gwupe.Communication.P2P.P2P.Socket;
using log4net;

namespace Gwupe.Agent.Components.Functions.RemoteDesktop
{
    internal class Client : ClientImpl
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Client));
        private StreamProxy _proxy;

        public IPEndPoint LocalEndPoint { get; private set; }

        internal Client(Attendance secondParty) : base(secondParty)
        {
        }

        internal int Start(String connectionId)
        {
            // First we need p2p connection
            Socket = GwupeClientAppContext.CurrentAppContext.P2PManager.GetP2PConnection(SecondParty, connectionId);
            // Now we need to create a proxy
            var tcpSocket = new BmTcpSocket(new IPEndPoint(IPAddress.Any,0));
            tcpSocket.BindListen();
            _proxy = new StreamProxy(tcpSocket, Socket);
            Socket.ConnectionOpened += (sender, args) => { Closed = false; };
            Socket.ConnectionClosed += (sender, args) => Close();
            _proxy.Start();
            Thread.Sleep(5000); // sometimes we connect before the listener has started listening, pause here.
            LocalEndPoint = tcpSocket.LocalEndPoint;
            return tcpSocket.LocalEndPoint.Port;
        }

        internal bool Connected { get { return Socket != null && Socket.Connected; } }

        internal override void Close()
        {
            if (!Closing && !Closed)
            {
                Logger.Debug("Closing RemoteDesktop Proxy to local service");
                Closing = true;
                if (_proxy != null)
                {
                    _proxy.Close();
                }
                Closing = false;
                Closed = true;
                _proxy = null;
            }
        }

    }
}
