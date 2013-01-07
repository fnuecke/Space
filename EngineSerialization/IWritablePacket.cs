using System;
using System.Collections.Generic;

namespace Engine.Serialization
{
    /// <summary>
    /// Interface for packets/streams that can be written to.
    /// </summary>
    public interface IWritablePacket : IDisposable
    {
        /// <summary>
        /// The number of used bytes in the buffer.
        /// </summary>
        int Length { get; }

        /// <summary>
        /// Returns the underlying array buffer of this packet. This is a reference to
        /// the actually used buffer, so it should be treated as read-only.
        /// </summary>
        /// <returns>The raw contents of this packet as a <c>byte[]</c>.</returns>
        byte[] GetBuffer();

        /// <summary>
        /// Writes the specified boolean value.
        /// </summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        IWritablePacket Write(bool data);

        /// <summary>
        /// Writes the specified byte value.
        /// </summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        IWritablePacket Write(byte data);

        /// <summary>
        /// Writes the specified double value.
        /// </summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        IWritablePacket Write(double data);

        /// <summary>
        /// Writes the specified single value.
        /// </summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        IWritablePacket Write(float data);

        /// <summary>
        /// Writes the specified int32 value.
        /// </summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        IWritablePacket Write(int data);

        /// <summary>
        /// Writes the specified in64 value.
        /// </summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        IWritablePacket Write(long data);

        /// <summary>
        /// Writes the specified int16 value.
        /// </summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        IWritablePacket Write(short data);

        /// <summary>
        /// Writes the specified uint32 value.
        /// </summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        IWritablePacket Write(uint data);

        /// <summary>
        /// Writes the specified uint64 value.
        /// </summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        IWritablePacket Write(ulong data);

        /// <summary>
        /// Writes the specified uint16 value.
        /// </summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        IWritablePacket Write(ushort data);

        /// <summary>
        /// Writes the specified length from the specified byte array.
        /// <para>
        /// May be <c>null</c>.
        /// </para>
        /// </summary>
        /// <param name="data">The value to write.</param>
        /// <param name="offset">The offset at which to start reading.</param>
        /// <param name="length">The number of bytes to write.</param>
        /// <returns>
        /// This packet, for call chaining.
        /// </returns>
        IWritablePacket Write(byte[] data, int offset, int length);

        /// <summary>
        /// Writes the specified byte array.
        /// 
        /// <para>
        /// May be <c>null</c>.
        /// </para>
        /// </summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        IWritablePacket Write(byte[] data);

        /// <summary>
        /// Writes the specified packet.
        /// </summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        IWritablePacket Write(IWritablePacket data);

        /// <summary>
        /// Writes the specified string value using UTF8 encoding.
        /// </summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        IWritablePacket Write(string data);

        /// <summary>
        /// Writes the specified type using its assembly qualified name.
        /// </summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        IWritablePacket Write(Type data);
        
        /// <summary>
        /// Writes the specified collection of objects.
        /// 
        /// <para>
        /// Must byte read back using <see cref="Packet.ReadPacketizables{T}"/>.
        /// </para>
        /// 
        /// <para>
        /// May be <c>null</c>.
        /// </para>
        /// </summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        IWritablePacket Write<T>(ICollection<T> data) where T : class, IPacketizable;

        /// <summary>
        /// Writes the specified collection of objects.
        /// </summary>
        /// 
        /// <para>
        /// Must byte read back using <see cref="Packet.ReadPacketizablesWithTypeInfo{T}"/>.
        /// </para>
        /// 
        /// <para>
        /// May be <c>null</c>.
        /// </para>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        IWritablePacket WriteWithTypeInfo<T>(ICollection<T> data) where T : class, IPacketizable;
    }
}