using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Engine.Serialization
{
    /// <summary>
    ///     Serialization utility class, for packing basic types into a byte array and reading them back. Actual packet
    ///     structure is implied by the program structure, i.e. the caller must know what the next thing to read should be.
    /// </summary>
    public sealed class Packet : IEquatable<Packet>, IWritablePacket, IReadablePacket
    {
        #region Properties

        /// <summary>The number of bytes available for reading.</summary>
        public int Available
        {
            get { return (int) (_stream.Length - _stream.Position); }
        }

        /// <summary>The number of used bytes in the buffer.</summary>
        public int Length
        {
            get { return (int) _stream.Length; }
        }

        #endregion

        #region Fields

        /// <summary>The underlying memory stream used for buffering.</summary>
        [PacketizerIgnore]
        private MemoryStream _stream;

        #endregion

        #region Constructor

        /// <summary>Create a new, empty packet.</summary>
        public Packet()
        {
            _stream = new MemoryStream();
        }

        /// <summary>Create a new packet based on the given buffer, which will result in a read-only packet.</summary>
        /// <param name="data">The data to initialize the packet with.</param>
        /// <param name="writable">Whether this packet can be written to..</param>
        public Packet(byte[] data, bool writable)
        {
            _stream = new MemoryStream(data ?? new byte[0], writable);
        }

        /// <summary>Disposes this packet, freeing any memory it occupies.</summary>
        public void Dispose()
        {
            if (_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }
        }

        #endregion

        #region Buffer

        /// <summary>
        ///     Returns the underlying array buffer of this packet. This is a reference to the actually used buffer, so it
        ///     should be treated as read-only.
        /// </summary>
        /// <returns>
        ///     The raw contents of this packet as a <c>byte[]</c>.
        /// </returns>
        public byte[] GetBuffer()
        {
            return _stream.ToArray();
        }

        /// <summary>Reset set the read position, to read from the beginning once more.</summary>
        public void Reset()
        {
            _stream.Position = 0;
        }

        #endregion

        #region Writing

        /// <summary>Writes the specified boolean value.</summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        public IWritablePacket Write(bool data)
        {
            var bytes = BitConverter.GetBytes(data);
            _stream.Write(bytes, 0, bytes.Length);
            return this;
        }

        /// <summary>Writes the specified byte value.</summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        public IWritablePacket Write(byte data)
        {
            _stream.WriteByte(data);
            return this;
        }

        /// <summary>Writes the specified double value.</summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        public IWritablePacket Write(double data)
        {
            var bytes = BitConverter.GetBytes(data);
            _stream.Write(bytes, 0, bytes.Length);
            return this;
        }

        /// <summary>Writes the specified single value.</summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        public IWritablePacket Write(float data)
        {
            var bytes = BitConverter.GetBytes(data);
            _stream.Write(bytes, 0, bytes.Length);
            return this;
        }

        /// <summary>Writes the specified int32 value.</summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        public IWritablePacket Write(int data)
        {
            var bytes = BitConverter.GetBytes(data);
            _stream.Write(bytes, 0, bytes.Length);
            return this;
        }

        /// <summary>Writes the specified in64 value.</summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        public IWritablePacket Write(long data)
        {
            var bytes = BitConverter.GetBytes(data);
            _stream.Write(bytes, 0, bytes.Length);
            return this;
        }

        /// <summary>Writes the specified int16 value.</summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        public IWritablePacket Write(short data)
        {
            var bytes = BitConverter.GetBytes(data);
            _stream.Write(bytes, 0, bytes.Length);
            return this;
        }

        /// <summary>Writes the specified uint32 value.</summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        public IWritablePacket Write(uint data)
        {
            var bytes = BitConverter.GetBytes(data);
            _stream.Write(bytes, 0, bytes.Length);
            return this;
        }

        /// <summary>Writes the specified uint64 value.</summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        public IWritablePacket Write(ulong data)
        {
            var bytes = BitConverter.GetBytes(data);
            _stream.Write(bytes, 0, bytes.Length);
            return this;
        }

        /// <summary>Writes the specified uint16 value.</summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        public IWritablePacket Write(ushort data)
        {
            var bytes = BitConverter.GetBytes(data);
            _stream.Write(bytes, 0, bytes.Length);
            return this;
        }

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
        public IWritablePacket Write(byte[] data, int offset, int length)
        {
            if (data == null)
            {
                return Write(-1);
            }

            Write(length);
            _stream.Write(data, offset, length);
            return this;
        }

        #endregion

        #region Reading

        /// <summary>Reads a boolean value.</summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public bool ReadBoolean()
        {
            if (!HasBoolean())
            {
                throw new PacketException("Cannot read boolean.");
            }
            var bytes = new byte[sizeof (bool)];
            _stream.Read(bytes, 0, bytes.Length);
            return BitConverter.ToBoolean(bytes, 0);
        }

        /// <summary>Reads a byte value.</summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public byte ReadByte()
        {
            if (!HasByte())
            {
                throw new PacketException("Cannot read byte.");
            }
            return (byte) _stream.ReadByte();
        }

        /// <summary>Reads a single value.</summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public float ReadSingle()
        {
            if (!HasSingle())
            {
                throw new PacketException("Cannot read single.");
            }
            var bytes = new byte[sizeof (float)];
            _stream.Read(bytes, 0, bytes.Length);
            return BitConverter.ToSingle(bytes, 0);
        }

        /// <summary>Reads a double value.</summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public double ReadDouble()
        {
            if (!HasDouble())
            {
                throw new PacketException("Cannot read double.");
            }
            var bytes = new byte[sizeof (double)];
            _stream.Read(bytes, 0, bytes.Length);
            return BitConverter.ToDouble(bytes, 0);
        }

        /// <summary>Reads an int16 value.</summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public short ReadInt16()
        {
            if (!HasInt16())
            {
                throw new PacketException("Cannot read int16.");
            }
            var bytes = new byte[sizeof (short)];
            _stream.Read(bytes, 0, bytes.Length);
            return BitConverter.ToInt16(bytes, 0);
        }

        /// <summary>Reads an int32 value.</summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public int ReadInt32()
        {
            if (!HasInt32())
            {
                throw new PacketException("Cannot read int32.");
            }
            var bytes = new byte[sizeof (int)];
            _stream.Read(bytes, 0, bytes.Length);
            return BitConverter.ToInt32(bytes, 0);
        }

        /// <summary>Reads an int64 value.</summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public long ReadInt64()
        {
            if (!HasInt64())
            {
                throw new PacketException("Cannot read int64.");
            }
            var bytes = new byte[sizeof (long)];
            _stream.Read(bytes, 0, bytes.Length);
            return BitConverter.ToInt64(bytes, 0);
        }

        /// <summary>Reads a uint16 value.</summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public ushort ReadUInt16()
        {
            if (!HasUInt16())
            {
                throw new PacketException("Cannot read uint16.");
            }
            var bytes = new byte[sizeof (ushort)];
            _stream.Read(bytes, 0, bytes.Length);
            return BitConverter.ToUInt16(bytes, 0);
        }

        /// <summary>Reads a uint32 value.</summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public uint ReadUInt32()
        {
            if (!HasUInt32())
            {
                throw new PacketException("Cannot read uint32.");
            }
            var bytes = new byte[sizeof (uint)];
            _stream.Read(bytes, 0, bytes.Length);
            return BitConverter.ToUInt32(bytes, 0);
        }

        /// <summary>Reads a uint64 value.</summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public ulong ReadUInt64()
        {
            if (!HasUInt64())
            {
                throw new PacketException("Cannot read uint64.");
            }
            var bytes = new byte[sizeof (ulong)];
            _stream.Read(bytes, 0, bytes.Length);
            return BitConverter.ToUInt64(bytes, 0);
        }

        /// <summary>Reads a byte array.</summary>
        /// <param name="buffer">The buffer to write to.</param>
        /// <param name="offset">The offset to start writing at.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>The number of bytes read.</returns>
        public int ReadByteArray(byte[] buffer, int offset, int count)
        {
            if (!HasByteArray())
            {
                throw new PacketException("Cannot read byte[].");
            }
            var length = ReadInt32();
            if (length != count)
            {
                throw new PacketException("Expected array size does not match written array's size.");
            }
            return _stream.Read(buffer, offset, count);
        }

        /// <summary>
        ///     Reads a byte array.
        ///     <para/>
        ///     May return <c>null</c>.
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public byte[] ReadByteArray()
        {
            if (!HasByteArray())
            {
                throw new PacketException("Cannot read byte[].");
            }
            var length = ReadInt32();
            if (length < 0)
            {
                return null;
            }

            var bytes = new byte[length];
            _stream.Read(bytes, 0, bytes.Length);
            return bytes;
        }

        #endregion

        #region Peeking

        /// <summary>Reads a boolean value without moving ahead the read pointer.</summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public bool PeekBoolean()
        {
            var position = _stream.Position;
            var result = ReadBoolean();
            _stream.Position = position;
            return result;
        }

        /// <summary>Reads a byte value without moving ahead the read pointer.</summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public byte PeekByte()
        {
            var position = _stream.Position;
            var result = ReadByte();
            _stream.Position = position;
            return result;
        }

        /// <summary>Reads a single value without moving ahead the read pointer.</summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public float PeekSingle()
        {
            var position = _stream.Position;
            var result = ReadSingle();
            _stream.Position = position;
            return result;
        }

        /// <summary>Reads a double value without moving ahead the read pointer.</summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public double PeekDouble()
        {
            var position = _stream.Position;
            var result = ReadDouble();
            _stream.Position = position;
            return result;
        }

        /// <summary>Reads an int16 value without moving ahead the read pointer.</summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public short PeekInt16()
        {
            var position = _stream.Position;
            var result = ReadInt16();
            _stream.Position = position;
            return result;
        }

        /// <summary>Reads an int32 value without moving ahead the read pointer.</summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public int PeekInt32()
        {
            var position = _stream.Position;
            var result = ReadInt32();
            _stream.Position = position;
            return result;
        }

        /// <summary>Reads an int64 value without moving ahead the read pointer.</summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public long PeekInt64()
        {
            var position = _stream.Position;
            var result = ReadInt64();
            _stream.Position = position;
            return result;
        }

        /// <summary>Reads a uint16 value without moving ahead the read pointer.</summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public ushort PeekUInt16()
        {
            long position = _stream.Position;
            var result = ReadUInt16();
            _stream.Position = position;
            return result;
        }

        /// <summary>Reads a uint32 value without moving ahead the read pointer.</summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public uint PeekUInt32()
        {
            var position = _stream.Position;
            var result = ReadUInt32();
            _stream.Position = position;
            return result;
        }

        /// <summary>Reads a uint64 value without moving ahead the read pointer.</summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public ulong PeekUInt64()
        {
            var position = _stream.Position;
            var result = ReadUInt64();
            _stream.Position = position;
            return result;
        }

        /// <summary>
        ///     Reads a byte array without moving ahead the read pointer.
        ///     <para>
        ///         May return <c>null</c>.
        ///     </para>
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public byte[] PeekByteArray()
        {
            var position = _stream.Position;
            var result = ReadByteArray();
            _stream.Position = position;
            return result;
        }

        /// <summary>Reads a string value using UTF8 encoding without moving ahead the read pointer.</summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public string PeekString()
        {
            var position = _stream.Position;
            var result = this.ReadString();
            _stream.Position = position;
            return result;
        }

        #endregion

        #region Checking

        /// <summary>Determines whether enough data is available to read a boolean value.</summary>
        /// <returns>
        ///     <c>true</c> if there is enough data; otherwise, <c>false</c>.
        /// </returns>
        public bool HasBoolean()
        {
            return Available >= sizeof (bool);
        }

        /// <summary>Determines whether enough data is available to read a byte value.</summary>
        /// <returns>
        ///     <c>true</c> if there is enough data; otherwise, <c>false</c>.
        /// </returns>
        public bool HasByte()
        {
            return Available >= sizeof (byte);
        }

        /// <summary>Determines whether enough data is available to read a single value.</summary>
        /// <returns>
        ///     <c>true</c> if there is enough data; otherwise, <c>false</c>.
        /// </returns>
        public bool HasSingle()
        {
            return Available >= sizeof (float);
        }

        /// <summary>Determines whether enough data is available to read a double value.</summary>
        /// <returns>
        ///     <c>true</c> if there is enough data; otherwise, <c>false</c>.
        /// </returns>
        public bool HasDouble()
        {
            return Available >= sizeof (double);
        }

        /// <summary>Determines whether enough data is available to read an int16 value.</summary>
        /// <returns>
        ///     <c>true</c> if there is enough data; otherwise, <c>false</c>.
        /// </returns>
        public bool HasInt16()
        {
            return Available >= sizeof (short);
        }

        /// <summary>Determines whether enough data is available to read an in32 value.</summary>
        /// <returns>
        ///     <c>true</c> if there is enough data; otherwise, <c>false</c>.
        /// </returns>
        public bool HasInt32()
        {
            return Available >= sizeof (int);
        }

        /// <summary>Determines whether enough data is available to read an int64 value.</summary>
        /// <returns>
        ///     <c>true</c> if there is enough data; otherwise, <c>false</c>.
        /// </returns>
        public bool HasInt64()
        {
            return Available >= sizeof (long);
        }

        /// <summary>Determines whether enough data is available to read a uint16 value.</summary>
        /// <returns>
        ///     <c>true</c> if there is enough data; otherwise, <c>false</c>.
        /// </returns>
        public bool HasUInt16()
        {
            return Available >= sizeof (ushort);
        }

        /// <summary>Determines whether enough data is available to read a uint32 value.</summary>
        /// <returns>
        ///     <c>true</c> if there is enough data; otherwise, <c>false</c>.
        /// </returns>
        public bool HasUInt32()
        {
            return Available >= sizeof (uint);
        }

        /// <summary>Determines whether enough data is available to read a uint64 value.</summary>
        /// <returns>
        ///     <c>true</c> if there is enough data; otherwise, <c>false</c>.
        /// </returns>
        public bool HasUInt64()
        {
            return Available >= sizeof (ulong);
        }

        /// <summary>Determines whether enough data is available to read a byte array.</summary>
        /// <returns>
        ///     <c>true</c> if there is enough data; otherwise, <c>false</c>.
        /// </returns>
        public bool HasByteArray()
        {
            if (HasInt32())
            {
                return Available >= sizeof (int) + Math.Max(0, PeekInt32());
            }
            return false;
        }

        /// <summary>Determines whether enough data is available to read a string value.</summary>
        /// <returns>
        ///     <c>true</c> if there is enough data; otherwise, <c>false</c>.
        /// </returns>
        public bool HasString()
        {
            return HasByteArray();
        }

        #endregion

        #region Casting

        /// <summary>Casts a packet to a byte array. This allocates a new array.</summary>
        /// <param name="value">The packet to cast.</param>
        /// <returns>
        ///     A byte array representing the packet's internal buffer, or <c>null</c> if the packet itself was <c>null</c>.
        /// </returns>
        public static explicit operator byte[](Packet value)
        {
            if (value == null)
            {
                return null;
            }
            var buffer = value.GetBuffer();
            var length = value.Length;
            var result = new byte[length];
            buffer.CopyTo(result, 0);
            return result;
        }

        /// <summary>
        ///     Casts a byte array to a packet.
        ///     <para>The resulting packet will be read-only.</para>
        /// </summary>
        /// <param name="value">The byte array to cast.</param>
        /// <returns>
        ///     A packet using the byte array as its internal buffer, or <c>null</c> if the packet itself was <c>null</c>.
        /// </returns>
        public static explicit operator Packet(byte[] value)
        {
            return value == null ? null : new Packet(value, false);
        }

        #endregion

        #region Equality

        /// <summary>Tests for equality with the specified object.</summary>
        /// <param name="other">The object to test for equality with.</param>
        /// <returns>Whether this and the specified object are equal.</returns>
        public bool Equals(Packet other)
        {
            return other != null &&
                   other._stream.Length == _stream.Length &&
                   SafeNativeMethods.memcmp(other._stream.GetBuffer(), _stream.GetBuffer(), _stream.Length) == 0;
        }

        internal class SafeNativeMethods
        {
            /// <summary>Compares two byte arrays.</summary>
            /// <param name="b1">The first array.</param>
            /// <param name="b2">The second array.</param>
            /// <param name="count">The number of bytes to check.</param>
            /// <returns>Zero if the two are equal.</returns>
            [DllImport("msvcrt.dll")]
            internal static extern int memcmp(byte[] b1, byte[] b2, long count);

            private SafeNativeMethods() {}
        }

        #endregion
    }
}