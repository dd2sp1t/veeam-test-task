using System;
using System.Threading;
using System.Collections.Generic;

namespace FileConverter
{
    public class CFileConverter : IDisposable
    {
        private readonly CDataReader _reader;
        private readonly CDataWriter _writer;
        private readonly CGZipHelper _zipHelper;

        private readonly List<Thread> _threads;
        private readonly CBlockingQueue<Byte[]> _queueToWrite;
        private readonly CBlockingQueue<Byte[]> _queueToConvert;

        private Int32 _readerCount;
        private Int32 _writerCount;
        private Int32 _converterCount;
        private Int32 _exceptionCount;

        private readonly Int32 _timeout;
        private readonly Object _syncObj;
        private readonly Dictionary<String, Exception> _exceptions;

        private CFileConverter(String sourceFile, String resultFile, Int32 blockSize)
        {
            _reader = CDataReader.Create(sourceFile, blockSize);
            _writer = CDataWriter.Create(resultFile, blockSize);
            _zipHelper = CGZipHelper.Create();

            _threads = new List<Thread>();
            _queueToWrite = CBlockingQueue<Byte[]>.Create(Environment.ProcessorCount * 10);
            _queueToConvert = CBlockingQueue<Byte[]>.Create(Environment.ProcessorCount * 10);

            _readerCount = 0;
            _writerCount = 0;
            _converterCount = 0;
            _exceptionCount = 0;

            _timeout = 5000;
            _syncObj = new Object();
            _exceptions = new Dictionary<String, Exception>();
        }

        public static CFileConverter Create(String sourceFile, String resultFile, Int32 blockSize)
        {
            return new CFileConverter(sourceFile, resultFile, blockSize);
        }

        public void Compress()
        {
            ConvertFile(_reader.ReadToCompress, _zipHelper.Compress, _writer.WriteCompressedData);
        }

        public void Decompress()
        {
            ConvertFile(_reader.ReadToDecompress, _zipHelper.Decompress, _writer.WriteDecompressedData);
        }

        private void ConvertFile(Func<Byte[]> reader, Func<Byte[], Byte[]> converter, Action<Byte[]> writer)
        {
            Thread readerThread = new Thread(() => Read(reader));
            Thread writerThread = new Thread(() => Write(writer));

            _threads.Add(readerThread);
            _threads.Add(writerThread);

            Interlocked.Increment(ref _readerCount);
            Interlocked.Increment(ref _writerCount);

            for (Int32 i = 0; i < Environment.ProcessorCount; i++)
            {
                Thread converterThread = new Thread(() => Convert(converter));

                _threads.Add(converterThread);

                Interlocked.Increment(ref _converterCount);
            }

            foreach (Thread thread in _threads) thread.Start();
            foreach (Thread thread in _threads) thread.Join();

            lock (_syncObj)
            {
                if (_exceptions.Count > 0) throw new AggregateException(_exceptions.Values);
            }
        }

        private void Read(Func<Byte[]> reader)
        {
            Boolean NeedToEnqueue() => _exceptionCount == 0 && _converterCount > 0;

            try
            {
                while (NeedToEnqueue())
                {
                    Byte[] data = reader();

                    if (data == null) break;

                    TryEnqueueWhile(NeedToEnqueue, _queueToConvert, data);
                }
            }
            catch (Exception exception)
            {
                CollectException(exception);
            }
            
            Interlocked.Decrement(ref _readerCount);
        }

        private void Convert(Func<Byte[], Byte[]> converter)
        {
            Boolean NeedToDequeue() => _exceptionCount == 0 && (_readerCount > 0 || _queueToConvert.Count > 0);
            Boolean NeedToEnqueue() => _exceptionCount == 0 && _writerCount > 0;

            try
            {
                while (NeedToDequeue())
                {
                    _queueToConvert.TryDequeue(out Byte[] data, _timeout);

                    if (data == null) continue;

                    Byte[] converted = converter(data);

                    TryEnqueueWhile(NeedToEnqueue, _queueToWrite, converted);
                }
            }
            catch (Exception exception)
            {
                CollectException(exception);
            }
            
            Interlocked.Decrement(ref _converterCount);
        }

        private void Write(Action<Byte[]> writer)
        {
            Boolean NeedToDequeue() => _exceptionCount == 0 && (_converterCount > 0 || _queueToWrite.Count > 0);

            try
            {
                while (NeedToDequeue())
                {
                    _queueToWrite.TryDequeue(out Byte[] data, _timeout);

                    if (data == null) continue;

                    writer(data);
                }
            }
            catch (Exception exception)
            {
                CollectException(exception);
            }
            
            Interlocked.Decrement(ref _writerCount);
        }

        private void TryEnqueueWhile(Func<Boolean> predicate, CBlockingQueue<Byte[]> queue, Byte[] data)
        {
            Boolean wasEnqueued = false;

            while (!wasEnqueued && predicate())
	            wasEnqueued = queue.TryEnqueue(data, _timeout);
        }
        
        private void CollectException(Exception exception)
        {
            Interlocked.Increment(ref _exceptionCount);

            lock (_syncObj)
            {
                _exceptions.TryAdd(exception.StackTrace, exception);
            }
        }

        public void Dispose()
        {
            _reader.Dispose();
            _writer.Dispose();
            _queueToWrite.Dispose();
            _queueToConvert.Dispose();
        }
    }
}