using Gwupe.Communication.P2P.RUDP.Tunnel.API;

namespace Gwupe.Communication.P2P.RUDP.Socket.API
{
    public interface ITcpOverUdptSocket
    {
        ITcpTransportLayer Connection { get; }
        void Send(byte[] data, int length, int timeout);
        int Read(byte[] data, int maxRead);
        bool Closed { get; }
        bool Closing { get; }
        void Close();
    }
}
