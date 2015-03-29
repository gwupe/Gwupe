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
            List<String> returnValue = null;
            try
            {
                returnValue = channel.getServers();
            }
            finally
            {
                ((IClientChannel)channel).Close();
            }
            return returnValue;
        }

        public void saveServers(List<string> servers)
        {
            IGwupeService channel = _channelFactory.CreateChannel(); try
            {
                channel.saveServers(servers);
            }
            finally
            {
                ((IClientChannel)channel).Close();
            }
        }

        public bool VNCStartService()
        {
            IGwupeService channel = _channelFactory.CreateChannel();
            bool rv;
            try
            {
                rv = channel.VNCStartService();
            }
            finally
            {
                ((IClientChannel)channel).Close();
            }

            return rv;
        }

        public void Ping()
        {
            IGwupeService channel = _channelFactory.CreateChannel();
            try
            {
                channel.Ping();
            }
            finally
            {
                ((IClientChannel)channel).Close();
            }
        }

        public string HardwareFingerprint()
        {
            IGwupeService channel = _channelFactory.CreateChannel();
            String rv = null;
            try
            {
                rv = channel.HardwareFingerprint();
            }
            finally
            {
                ((IClientChannel)channel).Close();
            }

            return rv;
        }

        public void SetPreRelease(bool preRelease)
        {
            IGwupeService channel = _channelFactory.CreateChannel();
            try
            {
                channel.SetPreRelease(preRelease);
            }
            finally
            {
                ((IClientChannel)channel).Close();
            }
        }

        public void DisableAutoUpgrade(bool disableAutoUpgrade)
        {
            IGwupeService channel = _channelFactory.CreateChannel();
            try
            {
                channel.DisableAutoUpgrade(disableAutoUpgrade);
            }
            finally
            {
                ((IClientChannel)channel).Close();
            }
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
