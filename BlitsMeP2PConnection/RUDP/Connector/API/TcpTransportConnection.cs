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
        internal bool Closing { get; private set; }
        internal event EventHandler Closed;

        private void OnClosed()
        {
            EventHandler handler = Closed;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        internal TcpTransportConnection(ITcpOverUdptSocket socket)
        {
            _socket = socket;
            _transportReaderThread = new Thread(TransportReader) { IsBackground = true };
            _transportReaderThread.Name = "_transportReaderThread[" + _transportReaderThread.ManagedThreadId + "]";
        }

        protected abstract void _Start();

        public void Start()
        {
            _transportReaderThread.Start();
            _Start();
            Started = true;
        }

        protected abstract void _Close();

        public void Close()
        {
            if (!Closing && Started)
            {
                Closing = true;
                _Close();
#if(DEBUG)
                Logger.Debug("Closing tcp connection components");
#endif
#if(DEBUG)
                Logger.Debug("Closing Socket");
#endif
                _socket.Close();
                // Only abort the thread if we are not it.
                if (_transportReaderThread.IsAlive && !_transportReaderThread.Equals(Thread.CurrentThread))
                {
#if(DEBUG)
                    Logger.Debug("Shutting of forward proxy thread.");
#endif
                    _transportReaderThread.Abort();
                }
                Started = false;
                OnClosed();
            }
        }

        protected abstract bool ProcessTransportRead(byte[] data);

        private void TransportReader()
        {
            try
            {
                int read = -1;
                while (true)
                {
                    byte[] tmpRead = new byte[8192];
                    try
                    {
                        read = _socket.Read(tmpRead, tmpRead.Length);
                    }
                    catch (ObjectDisposedException e)
                    {
                        Logger.Info("Connection has been closed");
                        break;
                    }
                    if (read > 0)
                    {
#if(DEBUG)
                        Logger.Debug("Read " + read + " bytes from tcp layer socket, writing to upstream handler [md5=" +
                                     Util.getSingleton().getMD5Hash(tmpRead, 0, read) + "]");
#endif
                        try
                        {
                            if (read < 8192)
                            {
                                var data = new byte[read];
                                Array.Copy(tmpRead, data, read);
                                tmpRead = data;
                            }
                            if (!ProcessTransportRead(tmpRead)) break;
                        }
                        catch (Exception e)
                        {
                            Logger.Error(
                                "Processing a read from the transportManager failed, shutting down TcpConnection");
                            break;
                        }
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
                    this.Close();
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
            if (!Closing)
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
            } else
            {
                Logger.Debug("Cannot write data to Transport, connection is closing");
                return false;
            }
            return true;
        }
    }
}
