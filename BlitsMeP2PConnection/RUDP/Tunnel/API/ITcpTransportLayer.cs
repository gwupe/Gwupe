using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlitsMe.Communication.P2P.RUDP.Packet.TCP;
using BlitsMe.Communication.P2P.RUDP.Socket.API;
using BlitsMe.Communication.P2P.RUDP.Packet.API;

namespace BlitsMe.Communication.P2P.RUDP.Tunnel.API
{
    public interface ITcpTransportLayer : ITransportLayer
    {
        // The id of this connection
        byte ConnectionId { get; }
        // the id of the remote side connection
        byte RemoteConnectionId { get; }
        // the protocol in use (sliding window etc etc)
        byte ProtocolId { get; }
        // the upstream socket we write to and read from
        IInternalTcpOverUdptSocket Socket { get; }
        // is the connection in an established state
        bool Established { get; }
        // last sequence number sent
        ushort LastSeqSent { get; }
        // next sequence number to be sent
        ushort NextSeqToSend { get; }
        // this will be called by the tcp transport when a data packet is received, implement this to do something with that data packet
        void ProcessDataPacket(ITcpDataPacket packet);
        // upstream socket will call this method to send data to remote connection
        void SendData(byte[] data, int length, int timeout);
        // tcp transport will call this when it receives an ack packet for processing by this layer
        void ProcessAck(StandardAckPacket packet);
        // this event will be called when the connection is established
        event EventHandler ConnectionOpen;
        // this event will be called when the connection is closed
        event EventHandler ConnectionClose;
        // this is called when a disconnect packet is received
        void ProcessDisconnect(StandardDisconnectPacket packet);
        // this is when the connection is disconnected
        bool Disconnected { get; }
    }
}
