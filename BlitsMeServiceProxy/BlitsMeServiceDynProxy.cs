using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using BlitsMe.Service.ServiceHost;

namespace BlitsMe.Service.ServiceProxy
{
    public class BlitsMeServiceDynProxy : IBlitsMeService
    {
        private NetNamedPipeBinding binding = new NetNamedPipeBinding();
        private EndpointAddress endpoint = new EndpointAddress("net.pipe://localhost/BlitsMeService");
        private ChannelFactory<IBlitsMeService> channelFactory;
        public BlitsMeServiceDynProxy()
        {
            channelFactory = new ChannelFactory<IBlitsMeService>(binding, endpoint);
        }

        public List<string> getServers()
        {
            IBlitsMeService channel = channelFactory.CreateChannel();
            List<String> returnValue = channel.getServers();
            ((IClientChannel)channel).Close();
            return returnValue;
        }

        public void saveServers(List<string> servers)
        {
            IBlitsMeService channel = channelFactory.CreateChannel();
            channel.saveServers(servers);
            ((IClientChannel)channel).Close();
        }

        public bool tvncStartService()
        {
            IBlitsMeService channel = channelFactory.CreateChannel();
            bool rv = channel.tvncStartService();
            ((IClientChannel)channel).Close();

            return rv;
        }

        ~BlitsMeServiceDynProxy()
        {
            this.close();
        }

        public void close()
        {
            if (channelFactory != null)
            {
                try { channelFactory.Close(); }
                catch (Exception e) { }
            }
        }
    }
}
