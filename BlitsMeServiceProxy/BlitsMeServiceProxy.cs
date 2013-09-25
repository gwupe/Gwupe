using System;
using System.Collections.Generic;
using System.ServiceModel;
using BlitsMe.ServiceHost;

namespace BlitsMe.ServiceProxy
{
    public class BlitsMeServiceProxy : IBlitsMeService
    {
#if DEBUG
        public const String BuildMarker = "_Dev";
#else
        public const String BuildMarker = "";
#endif
        private readonly NetNamedPipeBinding _binding = new NetNamedPipeBinding();
        private EndpointAddress endpoint = new EndpointAddress("net.pipe://localhost/BlitsMeService" + BuildMarker);
        private readonly ChannelFactory<IBlitsMeService> _channelFactory;
        internal bool IsClosed { get; private set; }

        public BlitsMeServiceProxy()
        {
            _channelFactory = new ChannelFactory<IBlitsMeService>(_binding, endpoint);
        }

        public List<string> getServers()
        {
            IBlitsMeService channel = _channelFactory.CreateChannel();
            List<String> returnValue = channel.getServers();
            ((IClientChannel)channel).Close();
            return returnValue;
        }

        public void saveServers(List<string> servers)
        {
            IBlitsMeService channel = _channelFactory.CreateChannel();
            channel.saveServers(servers);
            ((IClientChannel)channel).Close();
        }

        public bool VNCStartService()
        {
            IBlitsMeService channel = _channelFactory.CreateChannel();
            bool rv = channel.VNCStartService();
            ((IClientChannel)channel).Close();

            return rv;
        }

        public void Ping()
        {
            IBlitsMeService channel = _channelFactory.CreateChannel();
            channel.Ping();
            ((IClientChannel)channel).Close();
        }

        public string HardwareFingerprint()
        {
            IBlitsMeService channel = _channelFactory.CreateChannel();
            String rv = channel.HardwareFingerprint();
            ((IClientChannel)channel).Close();

            return rv;
        }

        ~BlitsMeServiceProxy()
        {
            this.close();
        }

        public void close()
        {
            if (!IsClosed)
            {
                IsClosed = true;
                if (_channelFactory != null)
                {
                    try
                    {
                        _channelFactory.Close();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Failed to close service proxy channel factory : " + e.Message);
                    }
                }
            }
        }
    }
}
