using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BlitsMe.Communication.P2P.RUDP.Connector;
using BlitsMe.Communication.P2P.RUDP.Connector.API;
using BlitsMe.Communication.P2P.RUDP.Socket.API;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;
using BlitsMe.Common;
using log4net;

namespace BlitsMe.Agent.Components.Functions.FileSend
{
    internal class FileSendListener : TcpTransportListener
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(FileSendListener));
        private readonly FileSendInfo _fileInfo;
        private FileStream _fileStream;
        private BinaryWriter _binWriter;
        private long _dataWriteSize;
        public long DataWriteSize { get { return _dataWriteSize; } }
        public event EventHandler DataRead;
        public bool FileReceiveResult = false;

        public void OnDataRead(EventArgs e)
        {
            EventHandler handler = DataRead;
            if (handler != null) handler(this, e);
        }

        internal FileSendListener(ITransportManager transportManager, FileSendInfo fileInfo)
            : base(fileInfo.FileSendId, transportManager)
        {
            _fileInfo = fileInfo;
            this.ConnectionAccepted += OnConnectionAccepted;
            this.ConnectionClosed += OnConnectionClosed;
        }

        private void OnConnectionClosed(object sender, NamedConnectionEventArgs namedConnectionEventArgs)
        {
            // check file size and close file here
            if (_fileStream != null)
            {
                _fileStream.Flush(true);
                _binWriter.Close();
                if (_dataWriteSize == _fileInfo.FileSize)
                {
                    Logger.Debug("File transfer of " + _fileInfo.Filename + " complete, filesize looks good");
                    FileReceiveResult = true;
                }
                else
                {
                    Logger.Warn("File transfer of " + _fileInfo.Filename + " looks like it failed, expected file size is " + _fileInfo.FileSize + " but destination filesize is " + _dataWriteSize);
                }
            }
            else
            {
                Logger.Warn("Failed to close the filestream, it is null");
            }

        }

        private void OnConnectionAccepted(object sender, NamedConnectionEventArgs namedConnectionEventArgs)
        {
            // open the file here
            string pathDownload = OsUtils.IsWinVistaOrHigher ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", _fileInfo.Filename) 
                                      : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), _fileInfo.Filename);
            _fileInfo.FilePath = pathDownload;
            try
            {
                _fileStream = new FileStream(pathDownload, FileMode.OpenOrCreate);
                _binWriter = new BinaryWriter(_fileStream);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to open the filestream for file " + pathDownload + " : " + e.Message, e);
                this.Close();
            }
        }

        protected override TcpTransportConnection ProcessConnect(ITcpOverUdptSocket socket)
        {
            DefaultTcpTransportConnection connection = new DefaultTcpTransportConnection(socket, Reader);
            return connection;
        }

        private bool Reader(byte[] data, int length, TcpTransportConnection connection)
        {
            if (length > 0)
            {
                try
                {
                    _binWriter.Write(data,0,length);
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to write data to file " + _fileInfo.Filename + " : " + e.Message, e);
                    return false;
                }
                _dataWriteSize += length;
                OnDataRead(EventArgs.Empty);
            }
            return true;
        }
    }
}
