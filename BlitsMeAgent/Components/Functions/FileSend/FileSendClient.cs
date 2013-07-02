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
        private long _dataWriteSize;
        private Boolean _proceed = true;
        public long DataWriteSize
        {
            get { return _dataWriteSize; }
        }

        internal event EventHandler DataWritten;

        internal void OnDataWritten(EventArgs e)
        {
            EventHandler handler = DataWritten;
            if (handler != null) handler(this, e);
        }

        internal event EventHandler<FileSendCompleteEventArgs> SendFileComplete;

        private void OnSendFileComplete(FileSendCompleteEventArgs e)
        {
            var handler = SendFileComplete;
            if (handler != null) handler(this, e);
        }

        internal FileSendClient(ITransportManager transportManager)
        {
            _transportManager = transportManager;
        }

        internal void SendFile(FileSendInfo fileInfo)
        {
            try
            {
                FileStream fs = File.Open(fileInfo.FilePath, FileMode.Open);
                BinaryReader binReader = new BinaryReader(fs);
                try
                {
                    _transportConnection =
                        new DefaultTcpTransportConnection(_transportManager.TCPTransport.OpenConnection(fileInfo.FileSendId), ReadReply);
                    try
                    {
                        byte[] read;
                        do
                        {
                            read = binReader.ReadBytes(8192);
                            if (read.Length > 0)
                            {
                                _transportConnection.SendDataToTransportSocket(read,read.Length);
                                _dataWriteSize += read.Length;
                                OnDataWritten(EventArgs.Empty);
                            }
                        } while (read.Length > 0 && _proceed);
                        if (_proceed)
                        {
                            Logger.Debug("Completed file send of " + fileInfo.Filename);
                            OnSendFileComplete(new FileSendCompleteEventArgs() { Success = true });
                        } else
                        {
                            Logger.Info("File transfer of " + fileInfo.Filename + " was cancelled");
                            OnSendFileComplete(new FileSendCompleteEventArgs() { Success = false, Error = "File transfer was cancelled"});
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Failed to read " + fileInfo.FilePath + " : " + e.Message,e);
                        OnSendFileComplete(new FileSendCompleteEventArgs() { Error = "Failed to read the file", Success = false });
                    } finally
                    {
                        _transportConnection.Close();
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to connect to endpoint " + fileInfo.FileSendId + " : " + e.Message, e);
                    OnSendFileComplete(new FileSendCompleteEventArgs() { Error = "Failed to connect to peer", Success = false });
                }
                finally
                {
                    binReader.Close();
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to open the file " + fileInfo.FilePath + " : " + e.Message, e);
                OnSendFileComplete(new FileSendCompleteEventArgs() { Error = "Failed to open the local file", Success = false });
            }
        }

        private bool ReadReply(byte[] data, int length, TcpTransportConnection connection)
        {
            return true;
        }

        internal void Close()
        {
            _proceed = false;
        }

    }


    internal class FileSendCompleteEventArgs : EventArgs
    {
        internal String Error { get; set; }
        internal bool Success = true;
    }
}
