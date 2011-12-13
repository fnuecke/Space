using System;
using System.IO;
using System.Text;
using Engine.Math;

namespace Engine.Serialization
{
    /// <summary>
    /// Serialization utility class, for packing basic types into a byte array.
    /// </summary>
    public sealed class Packet : IDisposable
    {
        #region Properties

        /// <summary>
        /// The number of bytes available for reading.
        /// </summary>
        public int Available { get { return (int)(_stream.Length - _stream.Position); } }

        /// <summary>
        /// The number of used bytes in the buffer.
        /// </summary>
        public int Length { get { return (int)_stream.Length; } }

        #endregion

        #region Fields

        private MemoryStream _stream;

        private bool _disposed;

        #endregion

        #region Constructor

        /// <summary>
        /// Create a new packet.
        /// </summary>
        public Packet()
        {
            _stream = new MemoryStream();
        }

        /// <summary>
        /// Create a new packet based on the given buffer, which will
        /// result in a read-only packet.
        /// </summary>
        /// <param name="data">the data to initialize the packet with.</param>
        public Packet(byte[] data)
        {
            _stream = new MemoryStream((data == null) ? new byte[0] : data, false);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _stream.Dispose();
                _stream = null;
                _disposed = true;
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Resets this packet, setting length and read/write position to zero.
        /// </summary>
        public void Reset()
        {
            _stream.SetLength(0);
            _stream.Position = 0;
        }

        /// <summary>
        /// Restart reading from the beginning.
        /// </summary>
        public void Rewind()
        {
            _stream.Position = 0;
        }

        #endregion

        #region Buffer

        /// <summary>
        /// Writes the contents of this packet to an array.
        /// </summary>
        public byte[] GetBuffer()
        {
            return _stream.ToArray();
        }

        #endregion

        #region Writing

        public Packet Write(bool data)
        {
            byte[] bytes = BitConverter.GetBytes(data);
            _stream.Write(bytes, 0, bytes.Length);
            return this;
        }

        public Packet Write(byte data)
        {
            _stream.WriteByte(data);
            return this;
        }

        public Packet Write(double data)
        {
            byte[] bytes = BitConverter.GetBytes(data);
            _stream.Write(bytes, 0, bytes.Length);
            return this;
        }

        public Packet Write(Fixed data)
        {
            byte[] bytes = BitConverter.GetBytes(data.RawValue);
            _stream.Write(bytes, 0, bytes.Length);
            return this;
        }

        public Packet Write(float data)
        {
            byte[] bytes = BitConverter.GetBytes(data);
            _stream.Write(bytes, 0, bytes.Length);
            return this;
        }

        public Packet Write(int data)
        {
            byte[] bytes = BitConverter.GetBytes(data);
            _stream.Write(bytes, 0, bytes.Length);
            return this;
        }

        public Packet Write(long data)
        {
            byte[] bytes = BitConverter.GetBytes(data);
            _stream.Write(bytes, 0, bytes.Length);
            return this;
        }

        public Packet Write(short data)
        {
            byte[] bytes = BitConverter.GetBytes(data);
            _stream.Write(bytes, 0, bytes.Length);
            return this;
        }

        public Packet Write(uint data)
        {
            byte[] bytes = BitConverter.GetBytes(data);
            _stream.Write(bytes, 0, bytes.Length);
            return this;
        }

        public Packet Write(ushort data)
        {
            byte[] bytes = BitConverter.GetBytes(data);
            _stream.Write(bytes, 0, bytes.Length);
            return this;
        }

        public Packet Write(ulong data)
        {
            byte[] bytes = BitConverter.GetBytes(data);
            _stream.Write(bytes, 0, bytes.Length);
            return this;
        }

        public Packet Write(byte[] data)
        {
            if (data == null)
            {
                Write((ushort)0);
            }
            else
            {
                Write(data, data.Length);
            }
            return this;
        }

        public Packet Write(byte[] data, int length)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            if (length > ushort.MaxValue)
            {
                throw new ArgumentException("Data is too long.", "length");
            }
            else
            {
                Write((ushort)length);
                _stream.Write(data, 0, length);
            }
            return this;
        }

        public Packet Write(Packet data)
        {
            if (data == null)
            {
                Write((ushort)0);
            }
            else
            {
                Write(data.GetBuffer());
            }
            return this;
        }

        public Packet Write(string data)
        {
            Write(Encoding.UTF8.GetBytes(data));
            return this;
        }

        public Packet Write(FPoint data)
        {
            Write(data.X);
            Write(data.Y);
            return this;
        }

        public Packet Write<TPlayerData, TPacketizerContext>(IPacketizable<TPlayerData, TPacketizerContext> data)
            where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>
            where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
        {
            data.Packetize(this);
            return this;
        }

        #endregion

        #region Reading

        public bool ReadBoolean()
        {
            if (!HasBoolean())
            {
                throw new PacketException("Cannot read boolean.");
            }
            byte[] bytes = new byte[sizeof(bool)];
            _stream.Read(bytes, 0, bytes.Length);
            return BitConverter.ToBoolean(bytes, 0);
        }

        public byte ReadByte()
        {
            if (!HasByte())
            {
                throw new PacketException("Cannot read byte.");
            }
            return (byte)_stream.ReadByte();
        }

        public float ReadSingle()
        {
            if (!HasSingle())
            {
                throw new PacketException("Cannot read single.");
            }
            byte[] bytes = new byte[sizeof(float)];
            _stream.Read(bytes, 0, bytes.Length);
            return BitConverter.ToSingle(bytes, 0);
        }

        public double ReadDouble()
        {
            if (!HasDouble())
            {
                throw new PacketException("Cannot read double.");
            }
            byte[] bytes = new byte[sizeof(double)];
            _stream.Read(bytes, 0, bytes.Length);
            return BitConverter.ToDouble(bytes, 0);
        }

        public Fixed ReadFixed()
        {
            if (!HasFixed())
            {
                throw new PacketException("Cannot read fixed.");
            }
            byte[] bytes = new byte[sizeof(long)];
            _stream.Read(bytes, 0, bytes.Length);
            return Fixed.Create(BitConverter.ToInt64(bytes, 0), false);
        }

        public short ReadInt16()
        {
            if (!HasInt16())
            {
                throw new PacketException("Cannot read int16.");
            }
            byte[] bytes = new byte[sizeof(short)];
            _stream.Read(bytes, 0, bytes.Length);
            return BitConverter.ToInt16(bytes, 0);
        }

        public int ReadInt32()
        {
            if (!HasInt32())
            {
                throw new PacketException("Cannot read int32.");
            }
            byte[] bytes = new byte[sizeof(int)];
            _stream.Read(bytes, 0, bytes.Length);
            return BitConverter.ToInt32(bytes, 0);
        }

        public long ReadInt64()
        {
            if (!HasInt64())
            {
                throw new PacketException("Cannot read int64.");
            }
            byte[] bytes = new byte[sizeof(long)];
            _stream.Read(bytes, 0, bytes.Length);
            return BitConverter.ToInt64(bytes, 0);
        }

        public ushort ReadUInt16()
        {
            if (!HasUInt16())
            {
                throw new PacketException("Cannot read uint16.");
            }
            byte[] bytes = new byte[sizeof(ushort)];
            _stream.Read(bytes, 0, bytes.Length);
            return BitConverter.ToUInt16(bytes, 0);
        }

        public uint ReadUInt32()
        {
            if (!HasUInt32())
            {
                throw new PacketException("Cannot read uint32.");
            }
            byte[] bytes = new byte[sizeof(uint)];
            _stream.Read(bytes, 0, bytes.Length);
            return BitConverter.ToUInt32(bytes, 0);
        }

        public ulong ReadUInt64()
        {
            if (!HasUInt64())
            {
                throw new PacketException("Cannot read uint64.");
            }
            byte[] bytes = new byte[sizeof(ulong)];
            _stream.Read(bytes, 0, bytes.Length);
            return BitConverter.ToUInt64(bytes, 0);
        }

        public byte[] ReadByteArray()
        {
            if (!HasByteArray())
            {
                throw new PacketException("Cannot read byte[].");
            }
            byte[] bytes = new byte[ReadUInt16()];
            _stream.Read(bytes, 0, bytes.Length);
            return bytes;
        }

        public Packet ReadPacket()
        {
            if (!HasPacket())
            {
                throw new PacketException("Cannot read packet.");
            }
            return new Packet(ReadByteArray());
        }

        public string ReadString()
        {
            if (!HasString())
            {
                throw new PacketException("Cannot read string.");
            }
            return Encoding.UTF8.GetString(ReadByteArray());
        }

        public FPoint ReadFPoint()
        {
            FPoint result;
            result.X = ReadFixed();
            result.Y = ReadFixed();
            return result;
        }

        public void ReadPacketizable<TPlayerData, TPacketizerContext>(IPacketizable<TPlayerData, TPacketizerContext> packetizable, TPacketizerContext context)
            where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>
            where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
        {
            packetizable.Depacketize(this, context);
        }

        #endregion

        #region Peeking

        public bool PeekBoolean()
        {
            long position = _stream.Position;
            var result = ReadBoolean();
            _stream.Position = position;
            return result;
        }

        public byte PeekByte()
        {
            long position = _stream.Position;
            var result = ReadByte();
            _stream.Position = position;
            return result;
        }

        public float PeekSingle()
        {
            long position = _stream.Position;
            var result = ReadSingle();
            _stream.Position = position;
            return result;
        }

        public double PeekDouble()
        {
            long position = _stream.Position;
            var result = ReadDouble();
            _stream.Position = position;
            return result;
        }

        public Fixed PeekFixed()
        {
            long position = _stream.Position;
            var result = ReadFixed();
            _stream.Position = position;
            return result;
        }

        public short PeekInt16()
        {
            long position = _stream.Position;
            var result = ReadInt16();
            _stream.Position = position;
            return result;
        }

        public int PeekInt32()
        {
            long position = _stream.Position;
            var result = ReadInt32();
            _stream.Position = position;
            return result;
        }

        public long PeekInt64()
        {
            long position = _stream.Position;
            var result = ReadInt64();
            _stream.Position = position;
            return result;
        }

        public ushort PeekUInt16()
        {
            long position = _stream.Position;
            var result = ReadUInt16();
            _stream.Position = position;
            return result;
        }

        public uint PeekUInt32()
        {
            long position = _stream.Position;
            var result = ReadUInt32();
            _stream.Position = position;
            return result;
        }

        public ulong PeekUInt64()
        {
            long position = _stream.Position;
            var result = ReadUInt64();
            _stream.Position = position;
            return result;
        }

        public byte[] PeekByteArray()
        {
            long position = _stream.Position;
            var result = ReadByteArray();
            _stream.Position = position;
            return result;
        }

        public Packet PeekPacket()
        {
            long position = _stream.Position;
            var result = ReadPacket();
            _stream.Position = position;
            return result;
        }

        public string PeekString()
        {
            long position = _stream.Position;
            var result = ReadString();
            _stream.Position = position;
            return result;
        }

        #endregion

        #region Checking

        public bool HasBoolean()
        {
            return Available >= sizeof(bool);
        }

        public bool HasByte()
        {
            return Available >= sizeof(byte);
        }

        public bool HasSingle()
        {
            return Available >= sizeof(float);
        }

        public bool HasDouble()
        {
            return Available >= sizeof(double);
        }

        public bool HasFixed()
        {
            return Available >= sizeof(long);
        }

        public bool HasInt16()
        {
            return Available >= sizeof(short);
        }

        public bool HasInt32()
        {
            return Available >= sizeof(int);
        }

        public bool HasInt64()
        {
            return Available >= sizeof(long);
        }

        public bool HasUInt16()
        {
            return Available >= sizeof(ushort);
        }

        public bool HasUInt32()
        {
            return Available >= sizeof(uint);
        }

        public bool HasUInt64()
        {
            return Available >= sizeof(ulong);
        }

        public bool HasByteArray()
        {
            if (HasUInt16())
            {
                return Available >= sizeof(ushort) + PeekUInt16();
            }
            return false;
        }

        public bool HasPacket()
        {
            return HasByteArray();
        }

        public bool HasString()
        {
            return HasByteArray();
        }

        public bool HasFPoint()
        {
            return Available >= (sizeof(long) * 2);
        }

        #endregion
    }
}
