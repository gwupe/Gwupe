using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using BlitsMe.Cloud.Communication;
using Microsoft.Win32;
using BlitsMe.Service.ServiceHost;

namespace BlitsMe.Service
{
    public partial class BMService : ServiceBase
    {
        private CloudConnection connection;
        public List<String> servers;
        private System.ServiceModel.ServiceHost serviceHost;
        public BMService()
        {
            InitializeComponent();
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
            connection.close();
            // TODO log something to event log
            // BlitsMeLog.logger.WriteEntry("BlitsMe Service Shutting Down");
        }

    }
}
