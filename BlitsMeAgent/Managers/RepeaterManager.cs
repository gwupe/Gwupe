using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BlitsMe.Agent.Components.Functions.Chat;
using BlitsMe.Agent.Components.RepeatedConnection;
using BlitsMe.Cloud.Communication;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using BlitsMe.Cloud.Repeater;
using log4net;
using log4net.Repository.Hierarchy;

namespace BlitsMe.Agent.Managers
{

    internal class RepeaterManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(RepeaterManager));
        private Dictionary<String, WaitingRepeatedConnection> _waitingConnections;

        public RepeaterManager()
        {
            _waitingConnections = new Dictionary<string, WaitingRepeatedConnection>();
        }

        internal void AddExpectedRepeatedConnection(String repeatId, Action<String, CoupledConnection> connectionEstablishedCallback, Func<MemoryStream, bool> readDataCallback)
        {
            WaitingRepeatedConnection connection = new WaitingRepeatedConnection() { RepeatId = repeatId, ConnectionEstablishedCallback = connectionEstablishedCallback, ReadDataCallback = readDataCallback };
            _waitingConnections.Add(repeatId, connection);
        }

        internal CoupledConnection GetRepeatedConnection(String repeatId)
        {
            WaitingRepeatedConnection pendingConnection;
            if (_waitingConnections.TryGetValue(repeatId, out pendingConnection))
            {
                var coupledConnection = BlitsMeClientAppContext.CurrentAppContext.ConnectionManager.StartRepeatedConnection(repeatId,
                    pendingConnection.ReadDataCallback);
                pendingConnection.ConnectionEstablishedCallback(repeatId, coupledConnection);
                return coupledConnection;
            }
            throw new Exception("Failed to find a couple connection for " + repeatId);
        }

        internal RepeatedConnection InitRepeatedConnection(String username, String shortCode, String interactionId, String repeatId)
        {
            InitRepeatedConnectionRq request = new InitRepeatedConnectionRq() { repeatId = repeatId, username = username, shortCode = shortCode, interactionId = interactionId };
            try
            {
                InitRepeatedConnectionRs response =
                    BlitsMeClientAppContext.CurrentAppContext.ConnectionManager.Connection
                        .Request<InitRepeatedConnectionRq, InitRepeatedConnectionRs>(request);

            }
            catch (Exception e)
            {
                Logger.Error("Failed to get a repeated connection for repeateId [" + repeatId + "] to " + username, e);
            }
            return null;
        }

    }
}
