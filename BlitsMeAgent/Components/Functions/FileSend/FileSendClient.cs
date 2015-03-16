using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Gwupe.Agent.Components.Functions.API;
using Gwupe.Agent.Components.Person;
using Gwupe.Cloud.Communication;
using Gwupe.Communication.P2P.P2P.Socket.API;
using Gwupe.Communication.P2P.RUDP.Connector;
using Gwupe.Communication.P2P.RUDP.Connector.API;
using Gwupe.Communication.P2P.RUDP.Tunnel.API;
using log4net;

namespace Gwupe.Agent.Components.Functions.FileSend
{
    internal class FileSendClient : ClientImpl
    {
        private readonly FileSendInfo _fileInfo;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(FileSendClient));
        private long _dataWriteSize;
        private CoupledConnection coupledConnection;

        public long DataWriteSize
        {
            get { return _dataWriteSize; }
        }

        internal event EventHandler<FileSendCompleteEventArgs> SendFileComplete;

        private void OnSendFileComplete(FileSendCompleteEventArgs e)
        {
            var handler = SendFileComplete;
            if (handler != null) handler(this, e);
        }

        internal event EventHandler DataWritten;

        internal void OnDataWritten(EventArgs e)
        {
            EventHandler handler = DataWritten;
            if (handler != null) handler(this, e);
        }

        internal FileSendClient(Attendance secondParty, FileSendInfo fileInfo) : base(secondParty)
        {
            _fileInfo = fileInfo;
        }

        internal void SendFile()
        {
            try
            {
                FileStream fs = File.Open(_fileInfo.FilePath, FileMode.Open);
                BinaryReader binReader = new BinaryReader(fs);
                try
                {
                    // First we need p2p connection
                    Socket = GwupeClientAppContext.CurrentAppContext.P2PManager.GetP2PConnection(SecondParty, _fileInfo.FileSendId);
                    Socket.ConnectionOpened += (sender, args) => { Closed = false; };
                    Socket.ConnectionClosed += (sender, args) => Close();
                    Socket.Connect();
                    try
                    {
                        byte[] read;
                        do
                        {
                            read = binReader.ReadBytes(8192);
                            if (read.Length > 0)
                            {
                                Socket.Send(read, read.Length);
                                //_dataWriteSize += read.Length;
                                _dataWriteSize = Socket.SentData;
                                OnDataWritten(EventArgs.Empty);
                            }
                        } while (read.Length > 0 && !Closing && !Closed);
                        if (!Closing && !Closed)
                        {
                            while (Socket.SentData < Socket.BufferedData)
                            {
                                Logger.Debug("sent = " + Socket.SentData + ", buffered = " + Socket.BufferedData);
                                if (_dataWriteSize != Socket.SentData)
                                {
                                    _dataWriteSize = Socket.SentData;
                                    OnDataWritten(EventArgs.Empty);
                                }
                                else
                                {
                                    Thread.Sleep(500);
                                }
                            }
                            Logger.Debug("Completed file send of " + _fileInfo.Filename);
                            OnSendFileComplete(new FileSendCompleteEventArgs() { Success = true });
                        } else
                        {
                            Logger.Info("File transfer of " + _fileInfo.Filename + " was cancelled");
                            OnSendFileComplete(new FileSendCompleteEventArgs() { Success = false, Error = "File transfer was cancelled"});
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Failed to read " + _fileInfo.FilePath + " : " + e.Message, e);
                        OnSendFileComplete(new FileSendCompleteEventArgs() { Error = "Failed to send the file", Success = false });
                    } 
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to connect to endpoint " + _fileInfo.FileSendId + " : " + e.Message, e);
                    OnSendFileComplete(new FileSendCompleteEventArgs() { Error = "Failed to connect to peer", Success = false });
                }
                finally
                {
                    binReader.Close();
                    Close();
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to open the file " + _fileInfo.FilePath + " : " + e.Message, e);
                OnSendFileComplete(new FileSendCompleteEventArgs() { Error = "Failed to open the local file", Success = false });
            }
        }

        internal override void Close()
        {
            if (!Closed && !Closing)
            {
                Closing = true;
                Socket.Close();
                Closing = false;
                Closed = true;
            }
        }

    }


    internal class FileSendCompleteEventArgs : EventArgs
    {
        internal String Error { get; set; }
        internal bool Success = true;
    }
}
