using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.ServiceProcess;
using System.Timers;
using Microsoft.Win32;
using BlitsMe.Service.ServiceHost;
using log4net;
using log4net.Config;

namespace BlitsMe.Service
{
    public partial class BMService : ServiceBase
    {
        private const int _updateCheckInterval = 3600;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(BMService));
        private readonly WebClient _webClient;
        private readonly Timer _updateCheck;
        // FIXME: Move this to a global config file at some point
        private const string tvncServiceName = "tvnserver";
        private const int tvnTimeoutMS = 30000;

        public List<String> Servers;
        private System.ServiceModel.ServiceHost serviceHost;
        public BMService()
        {
            XmlConfigurator.Configure();
            InitializeComponent();
            Logger.Info("BlitsMeService Starting Up");
            // Check for update on startup
            _webClient = new WebClient();
            CheckForNewVersion();
            // check for updates every interval
            _updateCheck = new Timer(_updateCheckInterval*1000);
            _updateCheck.Elapsed += delegate { CheckForNewVersion(); };
            _updateCheck.Start();
        }

        private void CheckForNewVersion()
        {
            try
            {
                String versionInfomation = _webClient.DownloadString("http://s1.i.dev.blits.me/updates/update.txt");
                String[] versionParts = versionInfomation.Split('\n')[0].Split(':');
                Version assemblyVersion = new Version(FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion);
                Version updateVersion = new Version(versionParts[0]);
                if (assemblyVersion.CompareTo(updateVersion) < 0)
                {
                    Logger.Debug("Upgrade Available : " + assemblyVersion + " => " + updateVersion);
                    try
                    {
                        Logger.Info("Downloading update " + versionParts[1]);
                        _webClient.DownloadFile("http://s1.i.dev.blits.me/updates/" + versionParts[1], System.IO.Path.GetTempPath() + "/" + versionParts[1]);
                        Logger.Info("Downloaded update " + versionParts[1]);
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Failed to download update : " + e.Message, e);
                    }
                } else
                {
                    Logger.Debug("No update available, current version " + assemblyVersion + ", available version " + updateVersion + ", checking again in " + (_updateCheckInterval/60) + " minutes.");
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to check for update : " + e.Message, e);
            }
        }

        protected override void OnStart(string[] args)
        {
            initServers();
            // do we need this connection?
            //connection = new CloudConnection(servers);
            serviceHost = new System.ServiceModel.ServiceHost(new BlitsMeService(this), new Uri("net.pipe://localhost/BlitsMeService"));
            serviceHost.Open();
        }

        private void initServers()
        {
            if (Servers == null || Servers.Count == 0)
            {
                try
                {
                    Servers = getServerIPs();
                    saveServerIPs(Servers);
                }
                catch (Exception e)
                {
                    // TODO Log something here to event log
                }
            }
        }

        public List<String> getServerIPs()
        {
            RegistryKey bmKey = Registry.LocalMachine.OpenSubKey(BLMRegistry.root);
            String ipKey = (String)bmKey.GetValue(BLMRegistry.serverIPsKey);
            return new List<String>(ipKey.Split(','));
        }

        public void saveServerIPs(List<String> newIPs)
        {
            // Lets add some
            try
            {
                RegistryKey ips = Registry.LocalMachine.CreateSubKey(BLMRegistry.root, RegistryKeyPermissionCheck.ReadWriteSubTree);
                ips.SetValue(BLMRegistry.serverIPsKey, String.Join(",", newIPs.ToArray()));
            }
            catch (Exception e2)
            {
                // TODO log something to event log
                // BlitsMeLog.logger.WriteEntry("Failed to determine server IP's from the registry [" + e2.GetType() + "] : " + e2.Message);
            }
        }

        public bool tvncStartService()
        {
            ServiceController service = new ServiceController(tvncServiceName);

            try
            {
                if (service.Status != ServiceControllerStatus.Running)
                {
                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(tvnTimeoutMS));
                }
            }
            catch (System.ServiceProcess.TimeoutException e)
            {
                Logger.Error("TightVNC service failed to start in a reasonable time : " + e.Message,e);
                return false;
            }
            catch (Exception e)
            {
                Logger.Error("TightVNC service failed to start : " + e.Message, e);
                return false;
            }

            return true;
        }

        protected override void OnStop()
        {
            serviceHost.Close();
            // TODO log something to event log
            // BlitsMeLog.logger.WriteEntry("BlitsMe Service Shutting Down");
        }

    }
}
