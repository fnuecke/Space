using System;
using System.IO;
using System.Net.Sockets;
using Engine.Serialization;

namespace Engine.Util
{
    public interface IPacketStream : IDisposable
    {
        /// <summary>
        /// Tries reading a single packet from this stream. To read multiple available
        /// packets, repeatedly call this method until it returns <c>null</c>.
        /// </summary>
        /// <returns>A read packet, if one was available.</returns>
        /// <exception cref="IOException">If the underlying stream fails.</exception>
        Packet Read();

        /// <summary>
        /// Writes the specified packet to the underlying stream. It can then be read
        /// byte a <c>PacketStream</c> on the other end of the stream.
        /// </summary>
        /// <param name="packet">The packet to write.</param>
        /// <exception cref="IOException">If the underlying stream fails.</exception>
        void Write(Packet packet);
    }

    /// <summary>
    /// Utility class for sending packets over a network stream.
    /// </summary>
    public abstract class AbstractPacketStream<T> : IPacketStream
        where T : Stream
    {
        #region Constants

        /// <summary>
        /// We'll just use the same key engine globally, as this isn't meant
        /// as a waterproof security anyways. Just make it easier to stay
        /// honest, so to say ;)
        /// </summary>
        private static readonly byte[] key = new byte[] { 58, 202, 84, 179, 32, 50, 8, 252, 238, 91, 233, 209, 25, 203, 183, 237, 33, 159, 103, 243, 93, 46, 67, 2, 169, 100, 96, 33, 196, 195, 244, 113 };

        /// <summary>
        /// Globally used initial vector.
        /// </summary>
        private static readonly byte[] vector = new byte[] { 112, 155, 187, 151, 110, 190, 166, 5, 137, 147, 104, 79, 199, 129, 24, 187 };

        /// <summary>
        /// Cryptography instance we'll use for mangling our packets.
        /// </summary>
        private static readonly SimpleCrypto crypto = new SimpleCrypto(key, vector);

        /// <summary>
        /// Bit set to mark a message as compressed.
        /// </summary>
        private const uint CompressedMask = 1u << 31;

        #endregion

        #region Properties

        /// <summary>
        /// Number of bytes available from our buffer.
        /// </summary>
        private int Available { get { return _bufferDataLength - _bufferReadPosition; } }

        #endregion

        #region Fields

        /// <summary>
        /// The underlying stream to read data from.
        /// </summary>
        private T _source;

        /// <summary>
        /// The underlying stream to write data to.
        /// </summary>
        private T _sink;

        /// <summary>
        /// Buffer for reading from the stream.
        /// </summary>
        private byte[] _buffer = new byte[512];

        /// <summary>
        /// The actual number of valid bytes in our buffer.
        /// </summary>
        private int _bufferDataLength;

        /// <summary>
        /// The position in our buffer we're currently reading from.
        /// </summary>
        private int _bufferReadPosition;

        /// <summary>
        /// Used to store any received data. This is used to build
        /// a single message (over and over).
        /// </summary>
        private MemoryStream _messageStream = new MemoryStream();

        /// <summary>
        /// Used to remember the length for the message we're currently
        /// building, so we don't have to use <c>BitConverter.ToInt32()</c>
        /// all the time (when continuing to read a partially received
        /// message).
        /// </summary>
        private int _messageLength;

        /// <summary>
        /// Remember if the message we're currently reading is compressed or not.
        /// </summary>
        private bool _isCompressed;

        #endregion

        #region Constructor / Cleanup

        protected AbstractPacketStream(T source, T sink)
        {
            this._source = source;
            this._sink = sink;
        }

        public void Dispose()
        {
            _messageStream.Dispose();
            _source.Dispose();
            _sink.Dispose();

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Read / Write

        /// <summary>
        /// Tries reading a single packet from this stream. To read multiple available
        /// packets, repeatedly call this method until it returns <c>null</c>.
        /// </summary>
        /// <returns>A read packet, if one was available.</returns>
        /// <exception cref="IOException">If the underlying stream fails.</exception>
        public Packet Read()
        {
            // Read until we either find a complete packet, or cannot read any more data.
            while (Available > 0 || IsDataAvailable(_source))
            {
                // Parse what's left in our buffer. If we find a packet, we return it.
                if (Available > 0)
                {
                    Packet packet = Parse();
                    if (packet != null)
                    {
                        return packet;
                    }
                }
                // Else we're guaranteed to be at the end of our buffer, and it wasn't
                // enough, so we get some more.
                if (IsDataAvailable(_source))
                {
                    // Get what we can fit in our buffer.
                    _bufferDataLength = _source.Read(_buffer, 0, _buffer.Length);
                    if (_bufferDataLength <= 0)
                    {
                        // Connection died (reading 0 bytes means the connection is gone).
                        throw new IOException();
                    }
                    // Reset our read position.
                    _bufferReadPosition = 0;
                }
            }
            // Got here means we failed.
            return null;
        }

        /// <summary>
        /// Writes the specified packet to the underlying stream. It can then be read
        /// byte a <c>PacketStream</c> on the other end of the stream.
        /// </summary>
        /// <param name="packet">The packet to write.</param>
        /// <exception cref="IOException">If the underlying stream fails.</exception>
        public void Write(Packet packet)
        {
            if (packet.Length > 0)
            {
                byte[] data = packet.GetBuffer();

                // If packets are large, try compressing them, see if it helps.
                // Only start after a certain size. General overhead for gzip
                // seems to be around 130byte, so make sure we're well beyond that.
                bool flag = false;
                if (data.Length > 200)
                {
                    byte[] compressed = SimpleCompression.Compress(data);
                    if (compressed.Length < data.Length)
                    {
                        // OK, worth it, it's smaller than before.
                        flag = true;
                        data = compressed;
                    }
                }

                // Encrypt the message.
                data = crypto.Encrypt(data);

                if ((data.Length & CompressedMask) > 0)
                {
                    throw new ArgumentException("Packet too long.", "packet");
                }

                _sink.Write(BitConverter.GetBytes((uint)data.Length | (flag ? CompressedMask : 0u)), 0, sizeof(uint));
                _sink.Write(data, 0, data.Length);
            }
        }

        #endregion

        #region Parsing

        /// <summary>
        /// Try to parse a message from the data that's currently in the buffer.
        /// </summary>
        private Packet Parse()
        {
            if (_messageLength <= 0)
            {
                // Message size unknown. Figure out how much more we need to read, and
                // read at most that much.
                int remainingSizeBytes = sizeof(int) - (int)_messageStream.Position;
                int sizeBytesToRead = System.Math.Min(Available, remainingSizeBytes);
                _messageStream.Write(_buffer, _bufferReadPosition, sizeBytesToRead);
                _bufferReadPosition += sizeBytesToRead;

                // Do we have enough data to figure out the size now?
                if (_messageStream.Position == sizeof(int))
                {
                    // Yes. Get the message length, compressed flag and read the remainder.
                    uint info = BitConverter.ToUInt32(_messageStream.GetBuffer(), 0);
                    _messageLength = (int)(info & ~CompressedMask);
                    _isCompressed = (info & CompressedMask) > 0;

                    if (_messageLength < 0)
                    {
                        // Invalid data, consider this connection broken.
                        throw new IOException();
                    }
                    // Reset so the rest is just the data.
                    _messageStream.SetLength(0);
                    _messageStream.Position = 0;

                    // Read up to the end of this message.
                    return Parse();
                } // else we also don't have anything else anymore.
            }
            else
            {
                // We already know our current message size. See if we can complete the
                // message.
                int remainingBodyBytes = _messageLength - (int)_messageStream.Position;
                int bodyBytesToRead = System.Math.Min(Available, remainingBodyBytes);
                _messageStream.Write(_buffer, _bufferReadPosition, bodyBytesToRead);
                _bufferReadPosition += bodyBytesToRead;

                // We done yet?
                if (_messageStream.Position == _messageLength)
                {
                    // Yep. Decrypt, decompress as necessary.
                    byte[] data = crypto.Decrypt(_messageStream.ToArray());
                    if (_isCompressed)
                    {
                        data = SimpleCompression.Decompress(data);
                    }

                    // Wrap up a packet, reset and return it.
                    Packet packet = new Packet(data);

                    // Reset for the next message.
                    _messageStream.SetLength(0);
                    _messageStream.Position = 0;
                    _messageLength = 0;
                    return packet;
                }
            }
            // Could not complete a packet.
            return null;
        }

        #endregion

        #region Internals

        protected abstract bool IsDataAvailable(T stream);

        #endregion
    }

    /// <summary>
    /// Implements a packet stream based on a <c>NetworkStream</c>.
    /// </summary>
    public sealed class NetworkPacketStream : AbstractPacketStream<NetworkStream>
    {
        public NetworkPacketStream(NetworkStream stream)
            : base(stream, stream)
        {
        }

        protected override bool IsDataAvailable(NetworkStream stream)
        {
            return stream.DataAvailable;
        }
    }

    /// <summary>
    /// Implements a packet stream based on a <c>SlidingStream</c>.
    /// </summary>
    public sealed class SlidingPacketStream : AbstractPacketStream<SlidingStream>
    {
        public SlidingPacketStream(SlidingStream source, SlidingStream sink)
            : base(source, sink)
        {
        }

        protected override bool IsDataAvailable(SlidingStream stream)
        {
            return stream.DataAvailable;
        }
    }
}
