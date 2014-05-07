using System;
using System.Net;

namespace BlitsMe.Communication.P2P.P2P.Socket.API
{
    public interface ISocket
    {
        void ListenOnce();
        void Connect();
        void Send(byte[] data, int length);
        int Read(byte[] data, int maxRead);
        int SendTimeout { get; set; }
        int ReadTimeout { get; set; }
        IPEndPoint RemoteEndPoint { get; }
        IPEndPoint LocalEndPoint { get; }
        void Close();
        bool Connected { get; }
        bool Closed { get; }
        bool Closing { get; }
        bool Listening { get; }

        int SentData { get; }
        int BufferedData { get; }

        event EventHandler ConnectionOpened;
        event EventHandler ConnectionClosed;
    }
}
