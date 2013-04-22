using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace BlitsMe.ServiceHost
{
    [ServiceContract]
    public interface IBlitsMeService
    {
        [OperationContract]
        List<string> getServers();

        [OperationContract]
        void saveServers(List<string> servers);

        [OperationContract]
        bool VNCStartService();

        [OperationContract]
        void Ping();

        [OperationContract]
        String HardwareFingerprint();
    }

    public interface IBlitsMeServiceChannel : IBlitsMeService, System.ServiceModel.IClientChannel
    {
    }

}
