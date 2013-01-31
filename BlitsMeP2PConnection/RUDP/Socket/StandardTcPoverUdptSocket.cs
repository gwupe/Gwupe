using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlitsMe.Communication.P2P.RUDP.Socket.API;
using BlitsMe.Common;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;
using BlitsMe.Communication.P2P.RUDP.Tunnel;
using System.IO;
using System.Threading;
using BlitsMe.Communication.P2P.Exceptions;

namespace BlitsMe.Communication.P2P.RUDP.Socket
{
    public class StandardTcpOverUdptSocket : IInternalTcpOverUdptSocket
    {
        public ITcpConnection Connection { get; private set; }
        private readonly AutoResetEvent _dataReady = new AutoResetEvent(false);
        private readonly Queue<byte[]> _clientBuffer;
        private byte[] _workingBuffer;
        private const int BufferSize = 100;
        public bool Closed { get; private set; }

        public StandardTcpOverUdptSocket(ITcpConnection connection)
        {
            this.Connection = connection;
            _clientBuffer = new Queue<byte[]>(BufferSize);
            Closed = false;
        }

        public void Send(byte[] data, int timeout)
        {
            Connection.SendData(data, timeout);
        }

        public void Close()
        {
            if (!Closed)
            {
                Closed = true;
                // to clear any blocked reads
                _dataReady.Set();
                Connection.Close();
            }
        }

        public int BufferClientData(byte[] data)
        {
            if (_clientBuffer.Count == BufferSize)
            {
                throw new BufferOverrunException("Socket buffer is full, cannot process more data");
            }
            lock (_dataReady)
            {
                _clientBuffer.Enqueue(data);
                _dataReady.Set();
            }
            return BufferSize - _clientBuffer.Count;
        }

        public int Read(byte[] data, int maxRead)
        {
            int readData = 0;
            // clear off current buffer
            if (_workingBuffer != null)
            {
                readData = ProcessWorkingBuffer(data, maxRead, readData);
            }
            if (readData < maxRead)
            {
                if (readData == 0 && !Closed)
                {
                    // we haven't read anything yet, we need to block till we do
                    _dataReady.WaitOne();
                }
                if (_clientBuffer.Count > 0)
                {
                    // there is stuff in the queue, lets have at it.
                    int queueElements = _clientBuffer.Count;
                    int counter = 0;
                    // Read off until we finish the known queue or maxRead is reached
                    while (counter < queueElements && readData < maxRead)
                    {
                        // we will only be processing the queue if the working buffer is empty. So lets pull the first element 
                        // into it for use
                        _workingBuffer = _clientBuffer.Dequeue();
                        readData = ProcessWorkingBuffer(data, maxRead - readData, readData);
                        counter++;
                    }
                    lock (_dataReady)
                    {
                        // We need to reset if we cleared all the data
                        if (_workingBuffer == null && _clientBuffer.Count == 0)
                        {
                            _dataReady.Reset();
                        }
                    }
                }
            }
            return readData;
        }

        private int ProcessWorkingBuffer(byte[] data, int maxRead, int readData)
        {
            if (_workingBuffer.Length > maxRead)
            {
                // workingBuffer is longer than we can currently manage
                // copy off what we can
                Array.Copy(_workingBuffer, 0, data, readData, maxRead);
                // maxed out the readData
                readData += maxRead;
                // now clear off the working buffer, what we have already read
                byte[] newWorkingBuffer = new byte[_workingBuffer.Length - maxRead];
                Array.Copy(_workingBuffer, maxRead, newWorkingBuffer, 0, _workingBuffer.Length - maxRead);
                _workingBuffer = newWorkingBuffer;
            }
            else
            {
                // We can use the whole buffer
                Array.Copy(_workingBuffer, 0, data, readData, _workingBuffer.Length);
                readData += _workingBuffer.Length;
                _workingBuffer = null;
            }
            return readData;
        }

    }
}
