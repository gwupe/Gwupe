using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace Gwupe.ServiceHost
{
    [ServiceContract]
    public interface IGwupeService
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

    public interface IGwupeServiceChannel : IGwupeService, IClientChannel
    {
    }

}
