using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlitsMe.Communication.P2P.RUDP.Connector.API
{
    public interface INamedConnector
    {
        // Connected
        event EventHandler<NamedConnectionEventArgs> ConnectionAccepted;
        // Disconnected
        event EventHandler<NamedConnectionEventArgs> ConnectionClosed;
        // Name of the connector you are connecting to
        String Name { get; }
    }
}
