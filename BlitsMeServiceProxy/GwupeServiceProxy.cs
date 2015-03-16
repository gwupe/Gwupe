using System;
using System.Collections.Generic;
using System.ServiceModel;
using Gwupe.ServiceHost;

namespace Gwupe.ServiceProxy
{
    public class GwupeServiceProxy : IGwupeService
    {
#if DEBUG
        public const String BuildMarker = "_Dev";
#else
        public const String BuildMarker = "";
#endif
        private readonly NetNamedPipeBinding _binding = new NetNamedPipeBinding();
        private EndpointAddress endpoint = new EndpointAddress("net.pipe://localhost/GwupeService" + BuildMarker);
        private readonly ChannelFactory<IGwupeService> _channelFactory;
        internal bool IsClosed { get; private set; }

        public GwupeServiceProxy()
        {
            _channelFactory = new ChannelFactory<IGwupeService>(_binding, endpoint);
        }

        public List<string> getServers()
        {
            IGwupeService channel = _channelFactory.CreateChannel();
            List<String> returnValue = channel.getServers();
            ((IClientChannel)channel).Close();
            return returnValue;
        }

        public void saveServers(List<string> servers)
        {
            IGwupeService channel = _channelFactory.CreateChannel();
            channel.saveServers(servers);
            ((IClientChannel)channel).Close();
        }

        public bool VNCStartService()
        {
            IGwupeService channel = _channelFactory.CreateChannel();
            bool rv = channel.VNCStartService();
            ((IClientChannel)channel).Close();

            return rv;
        }

        public void Ping()
        {
            IGwupeService channel = _channelFactory.CreateChannel();
            channel.Ping();
            ((IClientChannel)channel).Close();
        }

        public string HardwareFingerprint()
        {
            IGwupeService channel = _channelFactory.CreateChannel();
            String rv = channel.HardwareFingerprint();
            ((IClientChannel)channel).Close();

            return rv;
        }

        ~GwupeServiceProxy()
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
