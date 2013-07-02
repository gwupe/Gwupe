using System;
using System.Threading;

namespace BlitsMe.Communication.P2P.RUDP.Utils
{
    public class CircularBuffer<T>
    {
        private readonly T[] _array;
        private int _current = 0;
        private int _usedCount = 0;
        private readonly object _locker = new object();
        public int Count { get { return _usedCount; } }
        private bool _releaseGet;

        public CircularBuffer(int size)
        {
            _array = new T[size];
            _releaseGet = false;
        }

        public void Add(T[] additions, int length, int timeout = 0)
        {
            lock (_locker)
            {
                int added = 0;
                while (added < length)
                {
                    while (Available == 0)
                    {
                        if (timeout == 0)
                        {
                            Monitor.Wait(_locker);
                        }
                        else
                        {
                            if (!Monitor.Wait(_locker, timeout))
                                throw new TimeoutException("Failed to add units, timeout reached [" + timeout + "]");
                        }
                    }

                    // we have some space (we will add as much as we can)
                    int remainingLength = length - added;
                    int canAdd = Available < remainingLength ? Available : remainingLength;
                    int nextFree = (_current + _usedCount) % _array.Length;
                    int availableBeforeEnd = _array.Length - nextFree;
                    if (availableBeforeEnd >= canAdd)
                    {
                        // we don't need to split it, we have space before the end
                        Array.Copy(additions, added, _array, nextFree, canAdd);
                    }
                    else
                    {
                        Array.Copy(additions, added, _array, nextFree, availableBeforeEnd);
                        Array.Copy(additions, availableBeforeEnd + added, _array, 0, canAdd - availableBeforeEnd);
                    }
                    added += canAdd;
                    _usedCount = _usedCount + canAdd;
                    Monitor.PulseAll(_locker);
                }
            }
        }

        public T[] Get(int maxRunSize, int timeout = 0)
        {
            T[] retVal;
            _releaseGet = false;
            lock (_locker)
            {
                while (_usedCount == 0 && !_releaseGet)
                {
                    if (timeout == 0)
                    {
                        Monitor.Wait(_locker);
                    }
                    else
                    {
                        if (!Monitor.Wait(_locker, timeout))
                            throw new TimeoutException("Failed to get units, timeout reached [" + timeout + "]");
                    }
                }
                retVal = new T[maxRunSize > _usedCount ? _usedCount : maxRunSize];
                _get(maxRunSize, retVal);
            }
            return retVal;
        }


        public int Get(T[] retVal, int maxRead = 0, int timeout = 0)
        {
            if (maxRead == 0)
                maxRead = retVal.Length;
            _releaseGet = false;
            lock (_locker)
            {
                while (_usedCount == 0 && !_releaseGet)
                {
                    if (timeout == 0)
                    {
                        Monitor.Wait(_locker);
                    }
                    else
                    {
                        if (!Monitor.Wait(_locker, timeout))
                            throw new TimeoutException("Failed to get units, timeout reached [" + timeout + "]");
                    }
                }
                return _get(maxRead, retVal);
            }
        }

        private int _get(int maxRunSize, T[] retVal)
        {
            int returnLength = maxRunSize > _usedCount ? _usedCount : maxRunSize;
            if (returnLength > 0)
            {
                int distanceToEnd = _array.Length - _current;
                if (distanceToEnd < returnLength)
                {
                    // 2 copies
                    Array.Copy(_array, _current, retVal, 0, distanceToEnd);
                    Array.Copy(_array, 0, retVal, distanceToEnd, returnLength - distanceToEnd);
                    _current = returnLength - distanceToEnd;
                }
                else
                {
                    // 1 copy
                    Array.Copy(_array, _current, retVal, 0, returnLength);
                    _current = _current + returnLength;
                }
                _usedCount = _usedCount - returnLength;
            }
            if (_usedCount == 0)
            {
                _current = 0;
            }
            Monitor.PulseAll(_locker);
            return returnLength;
        }

        public int Available
        {
            get { return _array.Length - _usedCount; }
        }

        public void Release()
        {
            lock (_locker)
            {
                _releaseGet = true;
                Monitor.PulseAll(_locker);
            }
        }
    }
}
