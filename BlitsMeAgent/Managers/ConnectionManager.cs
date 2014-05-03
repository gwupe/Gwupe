using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Security.Policy;
using BlitsMe.Agent.Misc;
using BlitsMe.Cloud.Communication;
using BlitsMe.Cloud.Repeater;
using log4net;

namespace BlitsMe.Agent.Managers
{
    public class ConnectionManager
    {
#if DEBUG
        private const String Address = "i.dev.blits.me";
#else
        private static readonly String Address = "i.blits.me";
#endif
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ConnectionManager));
        private readonly BlitsMeClientAppContext _appContext;
        private readonly CloudConnection _connection;
        private readonly BLMRegistry _reg = new BLMRegistry();
        private X509Certificate2 _cert;
        internal bool IsClosed { get; private set; }
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

        public ConnectionManager()
        {
            _appContext = BlitsMeClientAppContext.CurrentAppContext;
            _connection = new CloudConnection();
            SaveServers(_connection.Servers);
        }

        public void Start()
        {
            _connection.StartConnection(_appContext.Version(), Cert);
        }

        public X509Certificate2 Cert
        {
            get
            {
                if (_cert == null)
                {
                    var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("BlitsMe.Agent.cacert.pem");
                    Byte[] certificateData = new Byte[stream.Length];
                    stream.Read(certificateData, 0, certificateData.Length);
                    _cert = new X509Certificate2(certificateData);
                    Logger.Info("Will use certificate from CA " + _cert.GetNameInfo(X509NameType.SimpleName, true) +
                                ", verified? " + _cert.Verify());
                }
                return _cert;
            }
        }

        public CoupledConnection StartRepeatedConnection(String repeatId, Func<MemoryStream, bool> readData)
        {
            CoupledConnection connection = new CoupledConnection(repeatId, Address, Cert, readData);
            return null;
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
            return _connection != null && _appContext.LoginManager.IsLoggedIn;
        }

        public void Close()
        {
            if (!IsClosed)
            {
                Logger.Debug("Closing ConnectionManager");
                IsClosed = true;
                if (_connection != null)
                {
                    _connection.Close();
                }
            }
        }

    }
}
