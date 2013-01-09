using System;
using System.IO;
using System.IO.Compression;

namespace Engine.Util
{
    /// <summary>Provides utility methods for compressing / decompressing raw data.</summary>
    public static class SimpleCompression
    {
        /// <summary>Compress binary data using GZIP.</summary>
        /// <param name="value">the raw, uncompressed data.</param>
        /// <returns>the compressed data.</returns>
        public static byte[] Compress(byte[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            return Compress(value, value.Length);
        }

        /// <summary>Compress binary data using GZIP.</summary>
        /// <param name="value">The raw, uncompressed data.</param>
        /// <param name="length">How far to read in the raw data.</param>
        /// <returns>The compressed data.</returns>
        public static byte[] Compress(byte[] value, int length)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            using (var output = new MemoryStream())
            {
                using (var gzip = new GZipStream(output, CompressionMode.Compress, true))
                {
                    gzip.Write(value, 0, length);
                }
                return output.ToArray();
            }
        }

        /// <summary>Decompresses binary data previously compressed using GZIP.</summary>
        /// <param name="value">The raw, compressed data.</param>
        public static byte[] Decompress(byte[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            return Decompress(value, 1024);
        }

        /// <summary>Decompresses binary data previously compressed using GZIP.</summary>
        /// <param name="value">The raw, compressed data.</param>
        /// <param name="bufferSize">Buffer size to use while decompressing.</param>
        /// <returns>the uncompressed data.</returns>
        public static byte[] Decompress(byte[] value, uint bufferSize)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            using (var input = new MemoryStream(value))
            {
                using (var gzip = new GZipStream(input, CompressionMode.Decompress))
                {
                    var buffer = new byte[bufferSize];
                    using (var output = new MemoryStream())
                    {
                        int count;
                        while ((count = gzip.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            output.Write(buffer, 0, count);
                        }
                        return output.ToArray();
                    }
                }
            }
        }
    }
}