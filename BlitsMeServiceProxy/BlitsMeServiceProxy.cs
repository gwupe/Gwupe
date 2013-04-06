using System;
using System.Collections.Generic;
using System.ServiceModel;
using BlitsMe.ServiceHost;

namespace BlitsMe.ServiceProxy
{
    public class BlitsMeServiceProxy : IBlitsMeService
    {
        private NetNamedPipeBinding binding = new NetNamedPipeBinding();
        private EndpointAddress endpoint = new EndpointAddress("net.pipe://localhost/BlitsMeService");
        private ChannelFactory<IBlitsMeService> channelFactory;
        public BlitsMeServiceProxy()
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

        public bool VNCStartService()
        {
            IBlitsMeService channel = channelFactory.CreateChannel();
            bool rv = channel.VNCStartService();
            ((IClientChannel)channel).Close();

            return rv;
        }

        ~BlitsMeServiceProxy()
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
