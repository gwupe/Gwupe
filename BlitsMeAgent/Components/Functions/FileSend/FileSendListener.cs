using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using BlitsMe.Agent.Components.Functions.API;
using BlitsMe.Communication.P2P.P2P.Socket.API;
using BlitsMe.Communication.P2P.RUDP.Connector;
using BlitsMe.Communication.P2P.RUDP.Connector.API;
using BlitsMe.Communication.P2P.RUDP.Socket.API;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;
using BlitsMe.Common;
using log4net;

namespace BlitsMe.Agent.Components.Functions.FileSend
{
    internal class FileSendListener : ServerImpl
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(FileSendListener));
        private readonly FileSendInfo _fileInfo;
        private Thread _fileReceiverThread;
        private long _dataReadSize;
        public long DataReadSize { get { return _dataReadSize; } }
        public event EventHandler DataRead;
        public bool FileReceiveResult = false;

        public void OnDataRead(EventArgs e)
        {
            EventHandler handler = DataRead;
            if (handler != null) handler(this, e);
        }

        internal FileSendListener(FileSendInfo fileInfo)
        {
            _fileInfo = fileInfo;
        }

        internal void Listen()
        {
            BlitsMeClientAppContext.CurrentAppContext.P2PManager.AwaitConnection(_fileInfo.FileSendId, ReceiveConnection);
        }

        private void ReceiveConnection(ISocket socket)
        {
            Socket = socket;
            Socket.ConnectionOpened += (sender, args) => { Closed = false; };
            Socket.ConnectionClosed += (sender, args) => Close();
            _fileReceiverThread = new Thread(ReceiveFile)
            {
                Name = "FileReceiver-" + _fileInfo.FileSendId,
                IsBackground = true
            };
            _fileReceiverThread.Start();
        }

        private void ReceiveFile()
        {
            try
            {
                Socket.ListenOnce();
                string pathDownload = OsUtils.IsWinVistaOrHigher ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", _fileInfo.Filename)
                                      : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), _fileInfo.Filename);
                FileStream fs = new FileStream(pathDownload, FileMode.OpenOrCreate);
                var binWriter = new BinaryWriter(fs);
                Stopwatch sw = new Stopwatch();
                try
                {
                    sw.Start();
                    var fsBuffer = new byte[16834];
                    int totalRead = 0;
                    int read = 0;
                    do
                    {
                        totalRead += read = Socket.Read(fsBuffer, (int)((_fileInfo.FileSize - totalRead > 16834) ? 16834 : _fileInfo.FileSize - totalRead));
                        _dataReadSize += read;
                        OnDataRead(EventArgs.Empty);
                        binWriter.Write(fsBuffer, 0, read);
                    } while (totalRead < _fileInfo.FileSize && !Closing && !Closed);
                    if (Closed || Closing)
                    {
                        throw new Exception("File Receiver was closed");
                    }
                    sw.Stop();
                    Logger.Info("File " + _fileInfo.Filename + " Received, " +
                                (_fileInfo.FileSize/1024).ToString("0") + " KB in " +
                                sw.Elapsed.TotalSeconds.ToString("##.##") + " seconds (" +
                                (sw.Elapsed.TotalSeconds == 0 ? "?" : (_fileInfo.FileSize/1024/sw.Elapsed.TotalSeconds).ToString("##.###")) + "KBps)");
                    FileReceiveResult = true;
                }
                catch (Exception ex)
                {
                    Logger.Error("Error occured while receiving file : " + ex.Message, ex);
                    sw.Stop();
                    Logger.Error("File " + _fileInfo.Filename + " failed, " + 
                        (_fileInfo.FileSize/1024).ToString("0") + " KB in " + 
                        sw.Elapsed.TotalSeconds.ToString("##.##") + " seconds (" + 
                        (sw.Elapsed.TotalSeconds == 0 ? "?" : (_fileInfo.FileSize / 1024 / sw.Elapsed.TotalSeconds).ToString("##.###")) + "KBps)");
                    FileReceiveResult = false;
                }
                finally
                {
                    fs.Flush();
                    binWriter.Close();
                    if (!FileReceiveResult)
                    {
                        // remove the file if it was incomplete
                        Logger.Warn("Removing incomplete file " + pathDownload);
                        try
                        {
                            File.Delete(pathDownload);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Failed to remove incomplete file : " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to receive file : " + ex.Message, ex);
            }
            finally
            {
                Close();
            }
        }

        internal override void Close()
        {
            if (!Closing && !Closed)
            {
                Logger.Debug("Closing File Listener");
                Closing = true;
                Socket.Close();
                Closing = false;
                Closed = true;
            }
        }

        /*
        private void OnConnectionClosed(object sender, NamedConnectionEventArgs namedConnectionEventArgs)
        {
            // check file size and close file here
            if (_fileStream != null)
            {
                _fileStream.Flush(true);
                _binWriter.Close();
                if (_dataReadSize == _fileInfo.FileSize)
                {
                    Logger.Debug("File transfer of " + _fileInfo.Filename + " complete, filesize looks good");
                    FileReceiveResult = true;
                }
                else
                {
                    Logger.Warn("File transfer of " + _fileInfo.Filename + " looks like it failed, expected file size is " + _fileInfo.FileSize + " but destination filesize is " + _dataReadSize);
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
                    _binWriter.Write(data, 0, length);
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to write data to file " + _fileInfo.Filename + " : " + e.Message, e);
                    return false;
                }
                _dataReadSize += length;
                OnDataRead(EventArgs.Empty);
            }
            return true;
        }
         * */

    }
}
