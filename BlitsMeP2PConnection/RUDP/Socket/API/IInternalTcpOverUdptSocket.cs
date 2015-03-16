using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gwupe.Communication.P2P.RUDP.Socket.API
{
    public interface IInternalTcpOverUdptSocket : ITcpOverUdptSocket
    {
        // method for the connection to buffer the data
        int BufferClientData(byte[] data);
    }
}
