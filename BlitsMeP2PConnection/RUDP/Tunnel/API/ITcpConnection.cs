using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlitsMe.Communication.P2P.RUDP.Packet.TCP;
using BlitsMe.Communication.P2P.RUDP.Socket.API;
using BlitsMe.Communication.P2P.RUDP.Packet.API;

namespace BlitsMe.Communication.P2P.RUDP.Tunnel.API
{
    public interface ITcpConnection : ITransportConnection
    {
        IInternalTcpOverUdptSocket socket { get; }
        int PacketCountReceiveAckValid { get; }
        int PacketCountReceiveAckInvalid { get; }
        int PacketCountReceiveDataFirst { get; }
        int PacketCountReceiveDataResend { get; }
        int PacketCountTransmitAckFirst { get; }
        int PacketCountTransmitAckResend { get; }
        int PacketCountTransmitDataFirst { get; }
        int PacketCountTransmitDataResend { get; }

        void ProcessDataPacket(ITcpPacket packet);
        void SendData(byte[] data, int timeout);
        void ProcessAck(StandardAckPacket packet);
    }
}
