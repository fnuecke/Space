using Engine.Serialization;
using Engine.Util;

namespace Engine.IO
{
    public sealed class CompressedPacketStream : IPacketStream
    {
        #region Fields
        
        /// <summary>
        /// The underlying stream.
        /// </summary>
        private readonly IPacketStream _stream;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="CompressedPacketStream"/> class.
        /// </summary>
        /// <param name="stream">The underlying stream to use.</param>
        public CompressedPacketStream(IPacketStream stream)
        {
            _stream = stream;
        }

        /// <summary>
        /// Disposes the underlying stream.
        /// </summary>
        public void Dispose()
        {
            _stream.Dispose();

            //GC.SuppressFinalize(this);
        }

        #endregion

        #region Interface

        /// <summary>
        /// Tries reading a single packet from this stream. To read multiple available
        /// packets, repeatedly call this method until it returns <c>null</c>.
        /// </summary>
        /// <returns>
        /// A read packet, if one was available.
        /// </returns>
        /// <exception cref="System.IO.IOException">If the underlying stream fails.</exception>
        public Packet Read()
        {
            using (var packet = _stream.Read())
            {
                if (packet != null)
                {
                    var compressed = packet.ReadBoolean();
                    var data = packet.ReadByteArray();
                    if (compressed)
                    {
                        data = SimpleCompression.Decompress(data);
                    }
                    return new Packet(data);
                }
            }
            return null;
        }

        /// <summary>
        /// Writes the specified packet to the underlying stream. It can then be read
        /// byte a <c>PacketStream</c> on the other end of the stream.
        /// </summary>
        /// <param name="packet">The packet to write.</param>
        /// <returns>The number of bytesa actually written. This can differ
        /// from the length of the specified packet due to transforms from
        /// wrapper streams (encryption, compression, ...)</returns>
        /// <exception cref="System.IO.IOException">If the underlying stream fails.</exception>
        public int Write(Packet packet)
        {
            using (var data = new Packet())
            {
                // If packets are large, try compressing them, see if it helps.
                // Only start after a certain size. General overhead for gzip
                // seems to be around 130byte, so make sure we're well beyond that.
                byte[] compressed;
                if (packet.Length > 200 && (compressed = SimpleCompression.Compress(packet.GetBuffer())).Length < packet.Length)
                {
                    // OK, worth it, it's smaller than before.
                    data.Write(true);
                    data.Write(compressed);
                }
                else
                {
                    data.Write(false);
                    data.Write(packet);
                }

                return _stream.Write(data);
            }
        }

        #endregion
    }
}
