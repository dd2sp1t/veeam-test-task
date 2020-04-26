using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace FileConverter
{
    internal class CDataReader : IDisposable
    {
        private readonly FileStream _fileStream;

        private readonly Byte[] _buffer;
        private readonly List<Byte> _currentResult;

        private Int32 _currentBlockNumber;

        private CDataReader(String path, Int32 blockSize)
        {
            _fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, blockSize * 10);

            _buffer = new Byte[blockSize];
            _currentResult = new List<Byte>(blockSize);

            _currentBlockNumber = 1;
        }

        public static CDataReader Create(String filePath, Int32 blockSize)
        {
            return new CDataReader(filePath, blockSize);
        }

        public Byte[] ReadToCompress()
        {
            Int32 bytesRead = _fileStream.Read(_buffer, 0, _buffer.Length);

            if (bytesRead == 0) return null;

            _currentResult.Clear();

            _currentResult.AddRange(_buffer.Take(bytesRead));
            _currentResult.AddRange(BitConverter.GetBytes(_currentBlockNumber));

            _currentBlockNumber++;

            return _currentResult.ToArray();
        }

        public Byte[] ReadToDecompress()
        {
            Int32 compressedBlockSize = ReadCompressedBlockSize();

            if (compressedBlockSize == 0) return null;

            _currentResult.Clear();

            while (0 < compressedBlockSize)
            {
                Int32 bytesRead = _fileStream.Read(_buffer, 0, Math.Min(_buffer.Length, compressedBlockSize));

                _currentResult.AddRange(_buffer.Take(bytesRead));

                compressedBlockSize -= bytesRead;
            }

            return _currentResult.ToArray();
        }

        private Int32 ReadCompressedBlockSize()
        {
            Int32 int32Size = 4;
            Byte[] bytes = new Byte[int32Size];
            Int32 bytesRead = _fileStream.Read(bytes, 0, bytes.Length);

            if (bytesRead == 0) return 0;

            if (bytesRead != int32Size)
                throw new InvalidOperationException("File corrupt. Could not read next block size.");

            Int32 result = BitConverter.ToInt32(bytes);

            if (result <= 0)
                throw new InvalidDataException("Invalid block size after converting.");

            return result;
        }

        public void Dispose()
        {
            _fileStream.Dispose();
        }
    }
}