using System.IO;
using System.IO.Compression;

namespace Engine.Util
{
    /// <summary>
    /// Provides utility methods for compressing / decompressing raw data.
    /// </summary>
    public static class Compression
    {
        /// <summary>
        /// Compress binary data using GZIP.
        /// </summary>
        /// <param name="raw">the raw, uncompressed data.</param>
        /// <param name="length">how far to read in the raw data.</param>
        /// <returns>the compressed data.</returns>
        public static byte[] Compress(byte[] raw, int length)
        {
            using (MemoryStream output = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(output, CompressionMode.Compress, true))
                {
                    gzip.Write(raw, 0, length);
                }
                return output.ToArray();
            }
        }

        /// <summary>
        /// Compress binary data using GZIP.
        /// </summary>
        /// <param name="raw">the raw, uncompressed data.</param>
        /// <returns>the compressed data.</returns>
        public static byte[] Compress(byte[] raw)
        {
            return Compress(raw, raw.Length);
        }

        /// <summary>
        /// Decompresses binary data previously compressed using GZIP.
        /// </summary>
        /// <param name="raw">the raw, compressed data.</param>
        /// <param name="bufferSize">buffer size to use while decompressing.</param>
        /// <returns>the uncompressed data.</returns>
        public static byte[] Decompress(byte[] raw, uint bufferSize = 1024)
        {
            using (GZipStream gzip = new GZipStream(new MemoryStream(raw), CompressionMode.Decompress))
            {
                byte[] buffer = new byte[bufferSize];
                using (MemoryStream output = new MemoryStream())
                {
                    int count = 0;
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
