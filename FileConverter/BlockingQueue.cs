using System;
using System.Threading;
using System.Collections.Generic;

namespace FileConverter
{
    internal class CBlockingQueue<T> : IDisposable where T : class
    {
        public Int32 Count
        {
            get
            {
                _mutex.WaitOne();

                try
                {
                    return _queue.Count;
                }
                finally
                {
                    _mutex.ReleaseMutex();
                }
            }
        }

        private readonly Queue<T> _queue;

        private readonly Mutex _mutex;
        private readonly Semaphore _producerSemaphore;
        private readonly Semaphore _consumerSemaphore;

        private CBlockingQueue(Int32 capacity)
        {
            _queue = new Queue<T>(capacity);

            _mutex = new Mutex();
            _producerSemaphore = new Semaphore(capacity, capacity);
            _consumerSemaphore = new Semaphore(0, capacity);
        }

        public static CBlockingQueue<T> Create(Int32 capacity)
        {
            return new CBlockingQueue<T>(capacity);
        }

        public Boolean TryEnqueue(in T item, in Int32 timeout = -1)
        {
            if (!_producerSemaphore.WaitOne(timeout)) return false;

            _mutex.WaitOne();

            try
            {
                _queue.Enqueue(item);
            }
            finally
            {
                _mutex.ReleaseMutex();
            }

            _consumerSemaphore.Release();

            return true;
        }

        public Boolean TryDequeue(out T item, in Int32 timeout = -1)
        {
            item = null;

            if (!_consumerSemaphore.WaitOne(timeout)) return false;

            _mutex.WaitOne();

            try
            {
                item = _queue.Dequeue();
            }
            finally
            {
                _mutex.ReleaseMutex();
            }

            _producerSemaphore.Release();

            return true;
        }

        public void Dispose()
        {
            _mutex.Dispose();
            _producerSemaphore.Dispose();
            _consumerSemaphore.Dispose();
        }
    }
}