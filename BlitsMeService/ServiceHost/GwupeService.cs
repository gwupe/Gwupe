using System;
using System.Collections.Generic;
using System.ServiceModel;
using Gwupe.ServiceHost;

namespace Gwupe.Service.ServiceHost
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class GwupeService : IGwupeService
    {
        private Service.GwupeService service;
        public GwupeService(Service.GwupeService gwupeService)
        {
            this.service = gwupeService;
        }

        public List<String> getServers()
        {
            return service.Servers;
        }

        public void saveServers(List<String> servers)
        {
            service.saveServerIPs(servers);
        }

        public bool VNCStartService()
        {
            return service.VNCStartService();
        }

        public void Ping()
        {
            service.Ping();
        }

        public string HardwareFingerprint()
        {
            return service.HardwareFingerprint();
        }

        public void SetPreRelease(bool preRelease)
        {
            service.SetPreRelease(preRelease);
        }

        public void DisableAutoUpgrade(bool disableAutoUpgrade)
        {
            service.DisableAutoUpgrade(disableAutoUpgrade);
        }
    }
}
