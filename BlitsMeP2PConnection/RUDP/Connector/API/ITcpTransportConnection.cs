namespace BlitsMe.Communication.P2P.RUDP.Connector.API
{
    public interface ITcpTransportConnection
    {
        void Close();
        bool SendDataToTransportSocket(byte[] data, int length);
    }
}