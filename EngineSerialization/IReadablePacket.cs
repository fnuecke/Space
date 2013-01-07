using System;

namespace Engine.Serialization
{
    /// <summary>
    /// Interface for packets/streams that can be read from.
    /// </summary>
    public interface IReadablePacket : IDisposable
    {
        /// <summary>
        /// The number of bytes available for reading.
        /// </summary>
        int Available { get; }

        /// <summary>
        /// The number of used bytes in the buffer.
        /// </summary>
        int Length { get; }

        /// <summary>
        /// Reads a boolean value.
        /// </summary>
        /// <param name="data">The read value.</param>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        /// <returns>This packet, for call chaining.</returns>
        IReadablePacket Read(out bool data);

        /// <summary>
        /// Reads a byte value.
        /// </summary>
        /// <param name="data">The read value.</param>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        /// <returns>This packet, for call chaining.</returns>
        IReadablePacket Read(out byte data);

        /// <summary>
        /// Reads a single value.
        /// </summary>
        /// <param name="data">The read value.</param>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        /// <returns>This packet, for call chaining.</returns>
        IReadablePacket Read(out float data);

        /// <summary>
        /// Reads a double value.
        /// </summary>
        /// <param name="data">The read value.</param>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        /// <returns>This packet, for call chaining.</returns>
        IReadablePacket Read(out double data);

        /// <summary>
        /// Reads an int16 value.
        /// </summary>
        /// <param name="data">The read value.</param>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        /// <returns>This packet, for call chaining.</returns>
        IReadablePacket Read(out short data);

        /// <summary>
        /// Reads an int32 value.
        /// </summary>
        /// <param name="data">The read value.</param>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        /// <returns>This packet, for call chaining.</returns>
        IReadablePacket Read(out int data);

        /// <summary>
        /// Reads an int64 value.
        /// </summary>
        /// <param name="data">The read value.</param>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        /// <returns>This packet, for call chaining.</returns>
        IReadablePacket Read(out long data);

        /// <summary>
        /// Reads a uint16 value.
        /// </summary>
        /// <param name="data">The read value.</param>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        /// <returns>This packet, for call chaining.</returns>
        IReadablePacket Read(out ushort data);

        /// <summary>
        /// Reads a uint32 value.
        /// </summary>
        /// <param name="data">The read value.</param>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        /// <returns>This packet, for call chaining.</returns>
        IReadablePacket Read(out uint data);

        /// <summary>
        /// Reads a uint64 value.
        /// </summary>
        /// <param name="data">The read value.</param>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        /// <returns>This packet, for call chaining.</returns>
        IReadablePacket Read(out ulong data);

        /// <summary>
        /// Reads a byte array.
        /// </summary>
        /// <param name="buffer">The buffer to write to.</param>
        /// <param name="offset">The offset to start writing at.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <param name="length">The actual number of bytes read.</param>
        /// <returns>This packet, for call chaining.</returns>
        IReadablePacket Read(byte[] buffer, int offset, int count, out int length);

        /// <summary>
        /// Reads a byte array.
        /// 
        /// <para>
        /// May yield <c>null</c>.
        /// </para>
        /// </summary>
        /// <param name="data">The read value.</param>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        /// <returns>This packet, for call chaining.</returns>
        IReadablePacket Read(out byte[] data);
        
        /// <summary>
        /// Reads a packet.
        /// 
        /// <para>
        /// May yield <c>null</c>.
        /// </para>
        /// </summary>
        /// <param name="data">The read value.</param>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        /// <returns>This packet, for call chaining.</returns>
        IReadablePacket Read(out IReadablePacket data);

        /// <summary>
        /// Reads a string value using UTF8 encoding.
        /// </summary>
        /// <param name="data">The read value</param>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        /// <returns>This packet, for call chaining.</returns>
        IReadablePacket Read(out string data);

        /// <summary>
        /// Reads a type value using its assembly qualified name for lookup.
        /// </summary>
        /// <param name="data">The read value.</param>
        /// <exception cref="PacketException">The type is not known in the
        /// local assembly.</exception>
        /// <returns>This packet, for call chaining.</returns>
        IReadablePacket Read(out Type data);

        /// <summary>
        /// Reads a boolean value.
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        bool ReadBoolean();

        /// <summary>
        /// Reads a byte value.
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        byte ReadByte();

        /// <summary>
        /// Reads a single value.
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        float ReadSingle();

        /// <summary>
        /// Reads a double value.
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        double ReadDouble();

        /// <summary>
        /// Reads an int16 value.
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        short ReadInt16();

        /// <summary>
        /// Reads an int32 value.
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        int ReadInt32();

        /// <summary>
        /// Reads an int64 value.
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        long ReadInt64();

        /// <summary>
        /// Reads a uint16 value.
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        ushort ReadUInt16();

        /// <summary>
        /// Reads a uint32 value.
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        uint ReadUInt32();

        /// <summary>
        /// Reads a uint64 value.
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        ulong ReadUInt64();

        /// <summary>
        /// Reads a byte array.
        /// </summary>
        /// <param name="buffer">The buffer to write to.</param>
        /// <param name="offset">The offset to start writing at.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>The number of bytes read.</returns>
        int ReadByteArray(byte[] buffer, int offset, int count);

        /// <summary>
        /// Reads a byte array.
        /// 
        /// <para>
        /// May return <c>null</c>.
        /// </para>
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        byte[] ReadByteArray();

        /// <summary>
        /// Reads a packet.
        /// 
        /// <para>
        /// May return <c>null</c>.
        /// </para>
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        IReadablePacket ReadPacket();

        /// <summary>
        /// Reads a string value using UTF8 encoding.
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        string ReadString();

        /// <summary>
        /// Reads a type value using its assembly qualified name for lookup.
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The type is not known in the
        /// local assembly.</exception>
        Type ReadType();

        /// <summary>
        /// Reads an object collections.
        /// 
        /// <para>
        /// May return <c>null</c>.
        /// </para>
        /// </summary>
        /// <typeparam name="T">The type of the objects to read.</typeparam>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        T[] ReadPacketizables<T>() where T : class, IPacketizable, new();

        /// <summary>
        /// Reads an object collections.
        /// 
        /// <para>
        /// May return <c>null</c>.
        /// </para>
        /// </summary>
        /// <typeparam name="T">Supertype of the type of the objects actually
        /// being read.</typeparam>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        T[] ReadPacketizablesWithTypeInfo<T>() where T : class, IPacketizable;

        /// <summary>
        /// Reads a boolean value without moving ahead the read pointer.
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        bool PeekBoolean();

        /// <summary>
        /// Reads a byte value without moving ahead the read pointer.
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        byte PeekByte();

        /// <summary>
        /// Reads a single value without moving ahead the read pointer.
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        float PeekSingle();

        /// <summary>
        /// Reads a double value without moving ahead the read pointer.
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        double PeekDouble();

        /// <summary>
        /// Reads an int16 value without moving ahead the read pointer.
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        short PeekInt16();

        /// <summary>
        /// Reads an int32 value without moving ahead the read pointer.
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        int PeekInt32();

        /// <summary>
        /// Reads an int64 value without moving ahead the read pointer.
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        long PeekInt64();

        /// <summary>
        /// Reads a uint16 value without moving ahead the read pointer.
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        ushort PeekUInt16();

        /// <summary>
        /// Reads a uint32 value without moving ahead the read pointer.
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        uint PeekUInt32();

        /// <summary>
        /// Reads a uint64 value without moving ahead the read pointer.
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        ulong PeekUInt64();

        /// <summary>
        /// Reads a byte array without moving ahead the read pointer.
        /// 
        /// <para>
        /// May return <c>null</c>.
        /// </para>
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        byte[] PeekByteArray();

        /// <summary>
        /// Reads a string value using UTF8 encoding without moving ahead the read pointer.
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        string PeekString();

        /// <summary>
        /// Determines whether enough data is available to read a boolean value.
        /// </summary>
        /// <returns><c>true</c> if there is enough data; otherwise, <c>false</c>.</returns>
        bool HasBoolean();

        /// <summary>
        /// Determines whether enough data is available to read a byte value.
        /// </summary>
        /// <returns><c>true</c> if there is enough data; otherwise, <c>false</c>.</returns>
        bool HasByte();

        /// <summary>
        /// Determines whether enough data is available to read a single value.
        /// </summary>
        /// <returns><c>true</c> if there is enough data; otherwise, <c>false</c>.</returns>
        bool HasSingle();

        /// <summary>
        /// Determines whether enough data is available to read a double value.
        /// </summary>
        /// <returns><c>true</c> if there is enough data; otherwise, <c>false</c>.</returns>
        bool HasDouble();

        /// <summary>
        /// Determines whether enough data is available to read an int16 value.
        /// </summary>
        /// <returns><c>true</c> if there is enough data; otherwise, <c>false</c>.</returns>
        bool HasInt16();

        /// <summary>
        /// Determines whether enough data is available to read an in32 value.
        /// </summary>
        /// <returns><c>true</c> if there is enough data; otherwise, <c>false</c>.</returns>
        bool HasInt32();

        /// <summary>
        /// Determines whether enough data is available to read an int64 value.
        /// </summary>
        /// <returns><c>true</c> if there is enough data; otherwise, <c>false</c>.</returns>
        bool HasInt64();

        /// <summary>
        /// Determines whether enough data is available to read a uint16 value.
        /// </summary>
        /// <returns><c>true</c> if there is enough data; otherwise, <c>false</c>.</returns>
        bool HasUInt16();

        /// <summary>
        /// Determines whether enough data is available to read a uint32 value.
        /// </summary>
        /// <returns><c>true</c> if there is enough data; otherwise, <c>false</c>.</returns>
        bool HasUInt32();

        /// <summary>
        /// Determines whether enough data is available to read a uint64 value.
        /// </summary>
        /// <returns><c>true</c> if there is enough data; otherwise, <c>false</c>.</returns>
        bool HasUInt64();

        /// <summary>
        /// Determines whether enough data is available to read a byte array.
        /// </summary>
        /// <returns><c>true</c> if there is enough data; otherwise, <c>false</c>.</returns>
        bool HasByteArray();

        /// <summary>
        /// Determines whether enough data is available to read a string value.
        /// </summary>
        /// <returns><c>true</c> if there is enough data; otherwise, <c>false</c>.</returns>
        bool HasString();
    }
}