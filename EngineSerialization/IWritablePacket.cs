using System;
using JetBrains.Annotations;

namespace Engine.Serialization
{
    /// <summary>Interface for packets/streams that can be written to.</summary>
    [PublicAPI]
    public interface IWritablePacket : IDisposable
    {
        #region Properties

        /// <summary>The number of used bytes in the buffer.</summary>
        [PublicAPI]
        int Length { get; }

        #endregion

        #region Buffer

        /// <summary>
        ///     Returns the underlying array buffer of this packet. This is a reference to the actually used buffer, so it
        ///     should be treated as read-only.
        /// </summary>
        /// <returns>
        ///     The raw contents of this packet as a <c>byte[]</c>.
        /// </returns>
        [PublicAPI]
        byte[] GetBuffer();

        /// <summary>Reset set the write position, to write from the beginning once more. This also resets the length to zero.</summary>
        [PublicAPI]
        void Reset();

        #endregion

        #region Writing

        /// <summary>Writes the specified boolean value.</summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        [PublicAPI]
        IWritablePacket Write(bool data);

        /// <summary>Writes the specified byte value.</summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        [PublicAPI]
        IWritablePacket Write(byte data);

        /// <summary>Writes the specified double value.</summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        [PublicAPI]
        IWritablePacket Write(double data);

        /// <summary>Writes the specified single value.</summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        [PublicAPI]
        IWritablePacket Write(float data);

        /// <summary>Writes the specified int32 value.</summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        [PublicAPI]
        IWritablePacket Write(int data);

        /// <summary>Writes the specified in64 value.</summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        [PublicAPI]
        IWritablePacket Write(long data);

        /// <summary>Writes the specified int16 value.</summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        [PublicAPI]
        IWritablePacket Write(short data);

        /// <summary>Writes the specified uint32 value.</summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        [PublicAPI]
        IWritablePacket Write(uint data);

        /// <summary>Writes the specified uint64 value.</summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        [PublicAPI]
        IWritablePacket Write(ulong data);

        /// <summary>Writes the specified uint16 value.</summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        [PublicAPI]
        IWritablePacket Write(ushort data);

        /// <summary>
        ///     Writes the specified length from the specified byte array.
        ///     <para>
        ///         May be <c>null</c>.
        ///     </para>
        /// </summary>
        /// <param name="data">The value to write.</param>
        /// <param name="offset">The offset at which to start reading.</param>
        /// <param name="length">The number of bytes to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        [PublicAPI]
        IWritablePacket Write(byte[] data, int offset, int length);

        #endregion
    }
}