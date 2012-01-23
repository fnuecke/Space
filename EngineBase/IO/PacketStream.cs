using System;
using System.IO;
using System.Net.Sockets;
using Engine.Serialization;

namespace Engine.IO
{
    /// <summary>
    /// Implements a packet stream based on a <c>NetworkStream</c>.
    /// </summary>
    public sealed class NetworkPacketStream : AbstractPacketStream<NetworkStream>
    {
        /// <summary>
        /// Creates a new packet stream around a network stream.
        /// </summary>
        /// <param name="stream">The network stream to wrap around.</param>
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
        /// <summary>
        /// Creates a new packet stream around a sliding stream.
        /// </summary>
        /// <param name="source">The source stream (read from).</param>
        /// <param name="sink">The sink stream (write to).</param>
        public SlidingPacketStream(SlidingStream source, SlidingStream sink)
            : base(source, sink)
        {
        }

        protected override bool IsDataAvailable(SlidingStream stream)
        {
            return stream.DataAvailable;
        }
    }

    /// <summary>
    /// Utility class for sending packets over a network stream.
    /// </summary>
    public abstract class AbstractPacketStream<T> : IPacketStream
        where T : Stream
    {
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

        #endregion

        #region Constructor / Cleanup

        protected AbstractPacketStream(T source, T sink)
        {
            this._source = source;
            this._sink = sink;
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _messageStream.Dispose();
                _source.Dispose();
                _sink.Dispose();
            }
        }

        #endregion

        #region Read / Write

        /// <summary>
        /// Tries reading a single packet from this stream. To read multiple available
        /// packets, repeatedly call this method until it returns <c>null</c>.
        /// </summary>
        /// <returns>A read packet, if one was available.</returns>
        /// <exception cref="System.IO.IOException">If the underlying stream fails.</exception>
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
        /// <returns>The number of bytesa actually written. This can differ
        /// from the length of the specified packet due to transforms from
        /// wrapper streams (encryption, compression, ...)</returns>
        /// <exception cref="System.IO.IOException">If the underlying stream fails.</exception>
        public int Write(Packet packet)
        {
            if (packet.Length > 0)
            {
                _sink.Write(BitConverter.GetBytes(packet.Length), 0, sizeof(int));
                _sink.Write(packet.GetBuffer(), 0, packet.Length);
            }
            return sizeof(int) + packet.Length;
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
                    _messageLength = BitConverter.ToInt32(_messageStream.GetBuffer(), 0);

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
                    // Yes. Wrap up a packet, reset and return it.
                    Packet packet = new Packet(_messageStream.ToArray());

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
}
