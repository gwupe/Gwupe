using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

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
            return service.servers;
        }

        public void saveServers(List<String> servers)
        {
            service.saveServerIPs(servers);
        }

    }
}
