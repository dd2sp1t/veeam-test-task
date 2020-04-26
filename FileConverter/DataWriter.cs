using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace FileConverter
{
    internal class CDataWriter : IDisposable
    {
        private readonly FileStream _fileStream;

        private Int32 _currentBlockNumber;
        private readonly Dictionary<Int32, Byte[]> _arrayDictionary;

        private CDataWriter(String path, Int32 blockSize)
        {
            _fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, blockSize * 10);

            _currentBlockNumber = 1;
            _arrayDictionary = new Dictionary<Int32, Byte[]>();
        }

        public static CDataWriter Create(String path, Int32 blockSize)
        {
            return new CDataWriter(path, blockSize);
        }

        public void WriteCompressedData(Byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Could not write data - it's null or empty.");

            _fileStream.Write(BitConverter.GetBytes(data.Length));

            _fileStream.Write(data, 0, data.Length);
        }

        public void WriteDecompressedData(Byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Could not write data - it's null or empty.");

            Int32 blockNumber = BitConverter.ToInt32(data.TakeLast(4).ToArray());

            if (blockNumber <= 0)
                throw new InvalidDataException("Invalid block number after converting.");

            _arrayDictionary.Add(blockNumber, data);

            while (_arrayDictionary.TryGetValue(_currentBlockNumber, out Byte[] item))
            {
                _arrayDictionary.Remove(_currentBlockNumber);

                Int32 int32Size = 4;
                _fileStream.Write(item, 0, item.Length - int32Size);

                _currentBlockNumber++;
            }
        }

        public void Dispose()
        {
            _fileStream.Flush();
            _fileStream.Dispose();
        }
    }
}