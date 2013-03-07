using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using BlitsMe.Common.Security;
using BlitsMe.Communication.P2P.RUDP.Socket.API;
using log4net;

namespace BlitsMe.Communication.P2P.RUDP.Connector.API
{
    public abstract class TcpTransportConnection
    {

        private static readonly ILog Logger = LogManager.GetLogger(typeof(TcpTransportConnection));

        private readonly Thread _transportReaderThread;

        private readonly ITcpOverUdptSocket _socket;

        internal bool Started { get; private set; }
        internal event EventHandler Closed;

        private void OnClosed()
        {
            EventHandler handler = Closed;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        internal TcpTransportConnection(ITcpOverUdptSocket socket)
        {
            _socket = socket;
            _transportReaderThread = new Thread(StartTransportReader) { IsBackground = true };
            _transportReaderThread.Name = "_transportReaderThread[" + _transportReaderThread.ManagedThreadId + "]";
        }

        protected abstract void _Start();

        public void Start()
        {
            _transportReaderThread.Start();
            _Start();
            Started = true;
        }

        public void Close()
        {
            Close(false);
        }

        protected abstract void _Close(bool initiatedBySelf);

        internal void Close(bool initiatedBySelf)
        {
#if(DEBUG)
            Logger.Debug("Closing tcp connection components");
#endif
            if (!_socket.Closed)
            {
#if(DEBUG)
                Logger.Debug("Closing Socket");
#endif
                _socket.Close();
            }
            if (_transportReaderThread.IsAlive && !_transportReaderThread.Equals(Thread.CurrentThread))
            {
#if(DEBUG)
                Logger.Debug("Shutting of forward proxy thread.");
#endif
                _transportReaderThread.Abort();
            }
            _Close(initiatedBySelf);
            if (Started)
            {
                Started = false;
                OnClosed();
            }
        }

        protected abstract bool ProcessTransportRead(byte[] data);

        private void StartTransportReader()
        {
            try
            {
                int read = -1;
                while (read != 0)
                {
                    byte[] tmpRead = new byte[8192];
                    read = _socket.Read(tmpRead, tmpRead.Length);
                    if (read > 0)
                    {
#if(DEBUG)
                        Logger.Debug("Read " + read + " bytes from tcp layer socket, writing to upstream handler [md5=" + Util.getSingleton().getMD5Hash(tmpRead, 0, read) + "]");
#endif
                        try
                        {
                            if(read < 8192)
                            {
                                var data = new byte[read];
                                Array.Copy(tmpRead, data, read);
                                tmpRead = data;
                            }
                            if (!ProcessTransportRead(tmpRead)) break;
                        }
                        catch (Exception e)
                        {
                            Logger.Error("Processing a read from the transportManager failed, shutting down TcpConnection");
                            break;
                        }
                    }
                    else
                    {
#if(DEBUG)
                        Logger.Error("Incoming transportManager socket has closed");
#endif
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Connection has failed : " + ex.Message);
            }
            finally
            {
                try
                {
                    this.Close(true);
                }
                catch (Exception e)
                {
                    Logger.Error("Error while closing ProxyConnection : " + e.Message, e);
                }
            }
        }

        public bool SendDataToTransport(byte[] tmpRead, int read)
        {
            byte[] write = new byte[read];
            Array.Copy(tmpRead, 0, write, 0, read);
            return SendDataToTransport(write);
        }

        public bool SendDataToTransport(byte[] write)
        {
            if (!_socket.Closed)
            {
                _socket.Send(write, 30000);
            }
            else
            {
#if(DEBUG)
                Logger.Debug("Outgoing transportManager socket has been closed");
#endif
                return false;
            }
            return true;
        }
    }
}
