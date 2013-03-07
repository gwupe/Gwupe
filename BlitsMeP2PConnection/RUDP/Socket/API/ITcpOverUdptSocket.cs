using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;
using BlitsMe.Common;
using System.IO;

namespace BlitsMe.Communication.P2P.RUDP.Socket.API
{
    public interface ITcpOverUdptSocket
    {
        ITcpTransportLayer Connection { get; }
        void Send(byte[] data, int timeout);
        int Read(byte[] data, int maxRead);
        bool Closed { get; }
        void Close();
    }
}
