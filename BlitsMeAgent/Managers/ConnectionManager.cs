using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using BlitsMe.Agent.Misc;
using BlitsMe.Cloud.Communication;
using BlitsMe.Communication.P2P.Exceptions;
using log4net;

namespace BlitsMe.Agent.Managers
{
    public class ConnectionManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ConnectionManager));
        private readonly BlitsMeClientAppContext _appContext;
        private readonly CloudConnection _connection;
        private readonly BLMRegistry _reg = new BLMRegistry();
        public event ConnectionEvent Disconnect
        {
            add { _connection.Disconnect += value; }
            remove { _connection.Disconnect -= value; }
        }

        public event ConnectionEvent Connect
        {
            add { _connection.Connect += value; }
            remove { _connection.Connect -= value; }
        }

        public CloudConnection Connection
        {
            get
            {
                return _connection;
            }
        }

        public ConnectionManager(BlitsMeClientAppContext appContext)
        {
            this._appContext = appContext;
            _connection = new CloudConnection(GetServers());
            SaveServers(_connection.servers);
        }

        private void SaveServers(List<string> servers)
        {
            try
            {
                _appContext.BlitsMeServiceProxy.saveServers(servers);
            }
            catch (Exception e)
            {
                _reg.saveServerIPs(servers);
                Logger.Error("Failed to contact service for saving servers");
            }
        }

        private List<string> GetServers()
        {
            List<string> servers = null;
            try
            {
                servers = _appContext.BlitsMeServiceProxy.getServers();
            }
            catch (Exception e)
            {
                Logger.Error("Failed to contact service to get servers");
                servers = _reg.getServerIPs();
            }
            return servers;
        }

        public bool IsOnline()
        {
            return _connection != null && _connection.isLoggedIn;
        }

        public void Close()
        {
            if (_connection != null)
            {
                _connection.close();
            }
        }

    }
}
