using System;
using System.IO;
using System.Net.Sockets;
using Engine.Serialization;

namespace Engine.IO
{
    public sealed class PacketStream : Stream
    {
        /// <summary>The packet we're writing to / reading from.</summary>
        private readonly Packet _packet;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PacketStream"/> class.
        /// </summary>
        /// <param name="packet">The packet.</param>
        public PacketStream(Packet packet)
        {
            _packet = packet;
        }

        /// <summary>
        ///     When overridden in a derived class, clears all buffers for this stream and causes any buffered data to be
        ///     written to the underlying device.
        /// </summary>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        public override void Flush() {}

        /// <summary>When overridden in a derived class, sets the position within the current stream.</summary>
        /// <returns>The new position within the current stream.</returns>
        /// <param name="offset">
        ///     A byte offset relative to the <paramref name="origin"/> parameter.
        /// </param>
        /// <param name="origin">
        ///     A value of type <see cref="T:System.IO.SeekOrigin"/> indicating the reference point used to obtain the new
        ///     position.
        /// </param>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        /// <exception cref="T:System.NotSupportedException">
        ///     The stream does not support seeking, such as if the stream is
        ///     constructed from a pipe or console output.
        /// </exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <summary>When overridden in a derived class, sets the length of the current stream.</summary>
        /// <param name="value">The desired length of the current stream in bytes. </param>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        /// <exception cref="T:System.NotSupportedException">
        ///     The stream does not support both writing and seeking, such as if the
        ///     stream is constructed from a pipe or console output.
        /// </exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     When overridden in a derived class, reads a sequence of bytes from the current stream and advances the
        ///     position within the stream by the number of bytes read.
        /// </summary>
        /// <returns>
        ///     The total number of bytes read into the buffer. This can be less than the number of bytes requested if that
        ///     many bytes are not currently available, or zero (0) if the end of the stream has been reached.
        /// </returns>
        /// <param name="buffer">
        ///     An array of bytes. When this method returns, the buffer contains the specified byte array with the values between
        ///     <paramref name="offset"/> and (<paramref name="offset"/> + <paramref name="count"/> - 1) replaced by the bytes read
        ///     from the current source.
        /// </param>
        /// <param name="offset">
        ///     The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data read from the current
        ///     stream.
        /// </param>
        /// <param name="count">The maximum number of bytes to be read from the current stream. </param>
        /// <exception cref="T:System.ArgumentException">
        ///     The sum of <paramref name="offset"/> and <paramref name="count"/> is larger than the buffer length.
        /// </exception>
        /// <exception cref="T:System.ArgumentNullException">
        ///     <paramref name="buffer"/> is null.
        /// </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///     <paramref name="offset"/> or <paramref name="count"/> is negative.
        /// </exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading. </exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
        public override int Read(byte[] buffer, int offset, int count)
        {
            return _packet.ReadByteArray(buffer, offset, count);
        }

        /// <summary>
        ///     When overridden in a derived class, writes a sequence of bytes to the current stream and advances the current
        ///     position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">
        ///     An array of bytes. This method copies <paramref name="count"/> bytes from <paramref name="buffer"/> to the current
        ///     stream.
        /// </param>
        /// <param name="offset">
        ///     The zero-based byte offset in <paramref name="buffer"/> at which to begin copying bytes to the current stream.
        /// </param>
        /// <param name="count">The number of bytes to be written to the current stream. </param>
        /// <exception cref="T:System.ArgumentException">
        ///     The sum of <paramref name="offset"/> and <paramref name="count"/> is greater than the buffer length.
        /// </exception>
        /// <exception cref="T:System.ArgumentNullException">
        ///     <paramref name="buffer"/> is null.
        /// </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///     <paramref name="offset"/> or <paramref name="count"/> is negative.
        /// </exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        /// <exception cref="T:System.NotSupportedException">The stream does not support writing. </exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
        public override void Write(byte[] buffer, int offset, int count)
        {
            _packet.Write(buffer, offset, count);
        }

        /// <summary>When overridden in a derived class, gets a value indicating whether the current stream supports reading.</summary>
        /// <returns>true if the stream supports reading; otherwise, false.</returns>
        public override bool CanRead
        {
            get { return true; }
        }

        /// <summary>When overridden in a derived class, gets a value indicating whether the current stream supports seeking.</summary>
        /// <returns>true if the stream supports seeking; otherwise, false.</returns>
        public override bool CanSeek
        {
            get { return false; }
        }

        /// <summary>When overridden in a derived class, gets a value indicating whether the current stream supports writing.</summary>
        /// <returns>true if the stream supports writing; otherwise, false.</returns>
        public override bool CanWrite
        {
            get { return true; }
        }

        /// <summary>When overridden in a derived class, gets the length in bytes of the stream.</summary>
        /// <returns>A long value representing the length of the stream in bytes.</returns>
        /// <exception cref="T:System.NotSupportedException">A class derived from Stream does not support seeking. </exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
        public override long Length
        {
            get { return _packet.Length; }
        }

        /// <summary>When overridden in a derived class, gets or sets the position within the current stream.</summary>
        /// <returns>The current position within the stream.</returns>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        /// <exception cref="T:System.NotSupportedException">The stream does not support seeking. </exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }
    }

    /// <summary>
    ///     Implements a packet stream based on a <c>NetworkStream</c>.
    /// </summary>
    public sealed class NetworkPacketStream : AbstractPacketStream<NetworkStream>
    {
        /// <summary>Creates a new packet stream around a network stream.</summary>
        /// <param name="stream">The network stream to wrap around.</param>
        public NetworkPacketStream(NetworkStream stream)
            : base(stream, stream) {}

        protected override bool IsDataAvailable(NetworkStream stream)
        {
            return stream.DataAvailable;
        }
    }

    /// <summary>
    ///     Implements a packet stream based on a <c>SlidingStream</c>.
    /// </summary>
    public sealed class SlidingPacketStream : AbstractPacketStream<SlidingStream>
    {
        /// <summary>Creates a new packet stream around a sliding stream.</summary>
        /// <param name="source">The source stream (read from).</param>
        /// <param name="sink">The sink stream (write to).</param>
        public SlidingPacketStream(SlidingStream source, SlidingStream sink)
            : base(source, sink) {}

        protected override bool IsDataAvailable(SlidingStream stream)
        {
            return stream.DataAvailable;
        }
    }

    /// <summary>Utility class for sending packets over a network stream.</summary>
    public abstract class AbstractPacketStream<T> : IPacketStream
        where T : Stream
    {
        #region Properties

        /// <summary>Number of bytes available from our buffer.</summary>
        private int Available
        {
            get { return _bufferDataLength - _bufferReadPosition; }
        }

        #endregion

        #region Fields

        /// <summary>The underlying stream to read data from.</summary>
        private readonly T _source;

        /// <summary>The underlying stream to write data to.</summary>
        private readonly T _sink;

        /// <summary>Buffer for reading from the stream.</summary>
        private readonly byte[] _buffer = new byte[512];

        /// <summary>The actual number of valid bytes in our buffer.</summary>
        private int _bufferDataLength;

        /// <summary>The position in our buffer we're currently reading from.</summary>
        private int _bufferReadPosition;

        /// <summary>Used to store any received data. This is used to build a single message (over and over).</summary>
        private readonly MemoryStream _messageStream = new MemoryStream();

        /// <summary>
        ///     Used to remember the length for the message we're currently building, so we don't have to use
        ///     <c>BitConverter.ToInt32()</c>
        ///     all the time (when continuing to read a partially received message).
        /// </summary>
        private int _messageLength;

        #endregion

        #region Constructor / Cleanup

        protected AbstractPacketStream(T source, T sink)
        {
            _source = source;
            _sink = sink;
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
        ///     Tries reading a single packet from this stream. To read multiple available packets, repeatedly call this method
        ///     until it returns <c>null</c>.
        /// </summary>
        /// <returns>A read packet, if one was available.</returns>
        /// <exception cref="System.IO.IOException">If the underlying stream fails.</exception>
        public IReadablePacket Read()
        {
            // Read until we either find a complete packet, or cannot read any more data.
            while (Available > 0 || IsDataAvailable(_source))
            {
                // Parse what's left in our buffer. If we find a packet, we return it.
                if (Available > 0)
                {
                    var packet = Parse();
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
        ///     Writes the specified packet to the underlying stream. It can then be read byte a <c>PacketStream</c> on the other
        ///     end of the stream.
        /// </summary>
        /// <param name="packet">The packet to write.</param>
        /// <returns>
        ///     The number of bytes actually written. This can differ from the length of the specified packet due to
        ///     transforms from wrapper streams (encryption, compression, ...)
        /// </returns>
        /// <exception cref="System.IO.IOException">If the underlying stream fails.</exception>
        public int Write(IWritablePacket packet)
        {
            if (packet.Length > 0)
            {
                _sink.Write(BitConverter.GetBytes(packet.Length), 0, sizeof (int));
                _sink.Write(packet.GetBuffer(), 0, packet.Length);
            }
            return sizeof (int) + packet.Length;
        }

        /// <summary>Flushes the underlying stream.</summary>
        /// <exception cref="System.IO.IOException">The underlying stream's Flush implementation caused an error.</exception>
        public void Flush()
        {
            _sink.Flush();
        }

        #endregion

        #region Parsing

        /// <summary>Try to parse a message from the data that's currently in the buffer.</summary>
        private IReadablePacket Parse()
        {
            if (_messageLength <= 0)
            {
                // Message size unknown. Figure out how much more we need to read, and
                // read at most that much.
                var remainingSizeBytes = sizeof (int) - (int) _messageStream.Position;
                var sizeBytesToRead = Math.Min(Available, remainingSizeBytes);
                _messageStream.Write(_buffer, _bufferReadPosition, sizeBytesToRead);
                _bufferReadPosition += sizeBytesToRead;

                // Do we have enough data to figure out the size now?
                if (_messageStream.Position == sizeof (int))
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
                var remainingBodyBytes = _messageLength - (int) _messageStream.Position;
                var bodyBytesToRead = Math.Min(Available, remainingBodyBytes);
                _messageStream.Write(_buffer, _bufferReadPosition, bodyBytesToRead);
                _bufferReadPosition += bodyBytesToRead;

                // We done yet?
                if (_messageStream.Position == _messageLength)
                {
                    // Yes. Wrap up a packet, reset and return it.
                    var packet = new Packet(_messageStream.ToArray(), false);

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