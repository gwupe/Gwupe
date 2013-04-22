using System;
using System.Collections.Generic;
using System.ServiceModel;
using BlitsMe.ServiceHost;

namespace BlitsMe.Service.ServiceHost
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class BlitsMeService : IBlitsMeService
    {
        private BMService service;
        public BlitsMeService(BMService bmService)
        {
            this.service = bmService;
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
    }
}
