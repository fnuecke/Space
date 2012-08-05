using Engine.Serialization;
using Engine.Util;

namespace Engine.IO
{
    /// <summary>
    /// Creates a layer of encryption around the specified packet stream.
    /// </summary>
    public sealed class EncryptedPacketStream : IPacketStream
    {
        #region Constants

        /// <summary>
        /// We'll just use the same key engine globally, as this isn't meant
        /// as a waterproof security anyways. Just make it easier to stay
        /// honest, so to say ;)
        /// </summary>
        private static readonly byte[] Key = new byte[] { 58, 202, 84, 179, 32, 50, 8, 252, 238, 91, 233, 209, 25, 203, 183, 237, 33, 159, 103, 243, 93, 46, 67, 2, 169, 100, 96, 33, 196, 195, 244, 113 };

        /// <summary>
        /// Globally used initial vector.
        /// </summary>
        private static readonly byte[] Vector = new byte[] { 112, 155, 187, 151, 110, 190, 166, 5, 137, 147, 104, 79, 199, 129, 24, 187 };

        /// <summary>
        /// Cryptography instance we'll use for mangling our packets.
        /// </summary>
        private static readonly SimpleCrypto Crypto = new SimpleCrypto(Key, Vector);

        #endregion

        #region Fields
        
        /// <summary>
        /// The underlying stream.
        /// </summary>
        private readonly IPacketStream _stream;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new encryption stream around the specified packet stream.
        /// </summary>
        /// <param name="stream"></param>
        public EncryptedPacketStream(IPacketStream stream)
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
                    return new Packet(Crypto.Decrypt(packet.ReadByteArray()));
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
                return _stream.Write(data.Write(Crypto.Encrypt(packet.GetBuffer())));
            }
        }

        /// <summary>
        /// Flushes the underlying stream.
        /// </summary>
        /// <exception cref="System.IO.IOException">The underlying stream's Flush implementation caused an error.</exception>
        public void Flush()
        {
            _stream.Flush();
        }

        #endregion
    }
}
