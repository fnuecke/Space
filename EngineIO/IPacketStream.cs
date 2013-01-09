using System;
using Engine.Serialization;

namespace Engine.IO
{
    /// <summary>A stream that operates on complete packets.</summary>
    public interface IPacketStream : IDisposable
    {
        /// <summary>
        ///     Tries reading a single packet from this stream. To read multiple available packets, repeatedly call this method
        ///     until it returns <c>null</c>.
        /// </summary>
        /// <returns>A read packet, if one was available.</returns>
        /// <exception cref="System.IO.IOException">If the underlying stream fails.</exception>
        IReadablePacket Read();

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
        int Write(IWritablePacket packet);

        /// <summary>Flushes the underlying stream.</summary>
        /// <exception cref="System.IO.IOException">The underlying stream's Flush implementation caused an error.</exception>
        void Flush();
    }
}