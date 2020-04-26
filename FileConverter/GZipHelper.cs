using System;
using System.IO;
using System.IO.Compression;

namespace FileConverter
{
    internal class CGZipHelper
    {
        public static CGZipHelper Create()
        {
            return new CGZipHelper();
        }

        public Byte[] Compress(Byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Could not compress data - it's null or empty.");

            using (MemoryStream targetStream = new MemoryStream())
            {
                using (GZipStream zipStream = new GZipStream(targetStream, CompressionMode.Compress))
                {
                    zipStream.Write(data, 0, data.Length);
                    zipStream.Flush();

                    return targetStream.ToArray();
                }
            }
        }

        public Byte[] Decompress(Byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Could not decompress data - it's null or empty.");

            using (MemoryStream originalStream = new MemoryStream(data))
            {
                using (MemoryStream targetStream = new MemoryStream())
                {
                    using (GZipStream zipStream = new GZipStream(originalStream, CompressionMode.Decompress))
                    {
                        zipStream.CopyTo(targetStream);

                        return targetStream.ToArray();
                    }
                }
            }
        }
    }
}