using System;
using System.Threading;
using Gwupe.Communication.P2P.P2P.Socket.API;
using Gwupe.Communication.P2P.RUDP.Socket.API;
using log4net;

namespace Gwupe.Communication.P2P.P2P.Connector
{
    public class StreamProxy
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(StreamProxy));
        private Thread _inRunner;
        private Thread _outRunner;
        private readonly ISocket _inStream;
        private readonly ISocket _outStream;
        private readonly int _bufferSize;
        public bool Closing { get; private set; }
        public bool Closed { get; private set; }

        public StreamProxy(ISocket inStream, ISocket outStream, int bufferSize = 16384)
        {
            Logger.Debug("Setting up Stream Proxy from " + inStream.GetType() + " to " + outStream.GetType() + "(bufsize=" + bufferSize + ")");
            _inStream = inStream;
            _outStream = outStream;
            _bufferSize = bufferSize;
        }

        public void Start()
        {
            AutoResetEvent _inRunnerReady = new AutoResetEvent(false);
            _inRunner = new Thread(() => ProxyThis(_inStream, _outStream, _inRunnerReady)) { Name = "proxyForwards", IsBackground = true };
            _outRunner = new Thread(() => ProxyThis(_outStream, _inStream, new AutoResetEvent(false))) { Name = "proxyReverse", IsBackground = true };
            _inRunner.Start();
            _inRunnerReady.WaitOne(30000);
        }

        private void ProxyThis(ISocket instream, ISocket outstream, AutoResetEvent readyToConnect)
        {
            Logger.Debug("Starting Proxy from " + instream + " to " + outstream);
            int read = 1;
            Closed = false;
            try
            {
                var inBuffer = new byte[_bufferSize];
                // listen for incoming connection
                if (!instream.Connected)
                {
                    readyToConnect.Set();
                    Logger.Debug("Listening for incoming connections");
                    instream.ListenOnce();
                    Logger.Debug("Connected incoming");
                }
                // connect to outgoing endpoint and start the reverse proxy
                if (!outstream.Connected)
                {
                    outstream.Connect();
                    Logger.Debug("Connected outgoing");
                    _outRunner.Start();
                    Logger.Debug("Started Reverse Proxy");
                }
                while (read > 0)
                {
                    // Read from instream
                    if (read > 0)
                    {
                        try
                        {
                            read = instream.Read(inBuffer, _bufferSize);
#if DEBUG
//                            Logger.Debug("Read " + read + " bytes from " + instream);
#endif
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Failed to read data from stream (" + instream + "), closing proxy : " + ex.Message);
                            break;
                        }
                        if (read > 0)
                        {
                            try
                            {
                                outstream.Send(inBuffer, read);
#if DEBUG
                                //Logger.Debug("Wrote " + read + " bytes to " + outstream);
#endif
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Failed to send data through stream (" + instream + ") , closing proxy : " + ex.Message);
                                break;
                            }
                        }
                    }
                    else
                    {
                        Logger.Warn("Read no data from (" + instream + "), closing proxy");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Thread caught exception while processing : " + ex.Message, ex);
            }
            finally
            {
                try
                {
                    instream.Close();
                }
                catch (Exception ex)
                {
                    Logger.Error("Caught Error closing stream (" + instream + ") : " + ex.Message, ex);
                }
                try
                {
                    outstream.Close();
                }
                catch (Exception ex)
                {
                    Logger.Error("Caught Error closing stream (" + outstream + ") : " + ex.Message, ex);
                }
            }
        }

        public void Close()
        {
            if (!Closing && !Closed)
            {
                Closing = true;
                try
                {
                    _inStream.Close();
                }
                catch (Exception ex)
                {
                    Logger.Error("Error occured closing instream",ex);
                }
                try
                {
                    _outStream.Close();
                }
                catch (Exception ex)
                {
                    Logger.Error("Error occured closing outstream", ex);
                }
                Closing = false;
                Closed = true;
            }
        }

    }
}
