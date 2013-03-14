using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BlitsMe.Communication.P2P.RUDP.Connector;
using BlitsMe.Communication.P2P.RUDP.Connector.API;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;
using log4net;

namespace BlitsMe.Agent.Components.Functions.FileSend
{
    internal class FileSendClient
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(FileSendClient));
        private readonly ITransportManager _transportManager;
        private DefaultTcpTransportConnection _transportConnection;

        internal event EventHandler SendFileComplete;

        private void OnSendFileComplete(FileSendCompleteEventArgs e)
        {
            EventHandler handler = SendFileComplete;
            if (handler != null) handler(this, e);
        }

        internal FileSendClient(ITransportManager transportManager)
        {
            _transportManager = transportManager;
        }

        internal void SendFile(String filePath, String fileSendId)
        {
            String filename = Path.GetFileName(filePath);
            try
            {
                FileStream fs = File.Open(filePath, FileMode.Open);
                BinaryReader binReader = new BinaryReader(fs);
                try
                {
                    _transportConnection =
                        new DefaultTcpTransportConnection(_transportManager.TCPTransport.OpenConnection(fileSendId), ReadReply);
                    _transportConnection.Start();
                    try
                    {
                        byte[] read;
                        do
                        {
                            read = binReader.ReadBytes(8192);
                            if (read.Length > 0)
                            {
                                _transportConnection.SendDataToTransport(read);
                            }
                        } while (read.Length > 0);
                        Logger.Debug("Completed file send of " + filePath);
                        OnSendFileComplete(new FileSendCompleteEventArgs() { Success = true });
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Failed to read " + filePath + " : " + e.Message,e);
                        OnSendFileComplete(new FileSendCompleteEventArgs() { Error = "Failed to read the file", Success = false });
                    } finally
                    {
                        _transportConnection.Close();
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to connect to endpoint " + filename + " : " + e.Message, e);
                    OnSendFileComplete(new FileSendCompleteEventArgs() { Error = "Failed to connect to peer", Success = false });
                }
                finally
                {
                    binReader.Close();
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to open the file " + filePath + " : " + e.Message, e);
                OnSendFileComplete(new FileSendCompleteEventArgs() { Error = "Failed to open the local file", Success = false });
            }
        }

        private bool ReadReply(byte[] data, TcpTransportConnection connection)
        {
            // Nothing will be sent back
            throw new NotImplementedException();
        }

    }

    internal class FileSendCompleteEventArgs : EventArgs
    {
        internal String Error { get; set; }
        internal bool Success = true;
    }
}
