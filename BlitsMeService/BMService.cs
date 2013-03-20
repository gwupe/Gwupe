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

namespace BlitsMe.Service
{
    public partial class BMService : ServiceBase
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(BMService));
        private WebClient webClient;
        private Timer updateCheck;

        public List<String> servers;
        private System.ServiceModel.ServiceHost serviceHost;
        public BMService()
        {
            InitializeComponent();
            Logger.Info("BlitsMeService Starting Up");
            updateCheck = new Timer(30000);
            updateCheck.Elapsed += CheckForNewVersion;
            updateCheck.Start();
        }

        private void CheckForNewVersion(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            if (webClient == null)
            {
                webClient = new WebClient();
            }
            try
            {
                String versionInfomation = webClient.DownloadString("http://s1.i.dev.blits.me/updates/update.txt");
                String[] versionParts = versionInfomation.Split('\n')[0].Split(':');
                Version assemblyVersion = new Version(FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion);
                Version updateVersion = new Version(versionParts[0]);
                if (assemblyVersion.CompareTo(updateVersion) < 0)
                {
                    Logger.Debug("Upgrade Available : " + assemblyVersion + " => " + updateVersion);
                    try
                    {
                        Logger.Info("Downloading update " + versionParts[1]);
                        webClient.DownloadFile("http://s1.i.dev.blits.me/updates/" + versionParts[1], System.IO.Path.GetTempPath() + "/" + versionParts[1]);
                        Logger.Info("Downloaded update " + versionParts[1]);
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Failed to download update : " + e.Message, e);
                    }
                } else
                {
                    Logger.Debug("No update available, current version " + assemblyVersion + ", available version " + updateVersion);
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
            if (servers == null || servers.Count == 0)
            {
                try
                {
                    servers = getServerIPs();
                    saveServerIPs(servers);
                }
                catch (Exception e)
                {
                    // TODO Log something here to event log
                }
            }
        }

        public static List<String> getServerIPs()
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


        protected override void OnStop()
        {
            serviceHost.Close();
            // TODO log something to event log
            // BlitsMeLog.logger.WriteEntry("BlitsMe Service Shutting Down");
        }

    }
}
