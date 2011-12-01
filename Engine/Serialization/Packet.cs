using System;
using System.Text;
using Engine.Math;

namespace Engine.Serialization
{
    /// <summary>
    /// Serialization utility class, for packing basic types into a byte array.
    /// </summary>
    public sealed class Packet
    {
        #region Properties

        /// <summary>
        /// The number of bytes available for reading.
        /// </summary>
        public int Available { get { return Length - readPointer; } }

        /// <summary>
        /// The underlying buffer.
        /// </summary>
        public byte[] Buffer { get; private set; }

        /// <summary>
        /// The number of used bytes in the buffer.
        /// </summary>
        public int Length { get; private set; }

        #endregion

        #region Fields

        /// <summary>
        /// Marks the current read / write position in the buffer.
        /// </summary>
        private int readPointer;

        #endregion

        #region Constructor

        /// <summary>
        /// Create a new packet of default buffer length (512).
        /// </summary>
        public Packet()
            : this(512)
        {
        }

        /// <summary>
        /// Create a new packet with a buffer of the given length.
        /// </summary>
        /// <param name="maxLength">the size of the used buffer.</param>
        public Packet(long maxLength)
        {
            Buffer = new byte[maxLength];
            Reset();
        }

        /// <summary>
        /// Create a new packet based on the given buffer (used directly, i.e. not copied).
        /// </summary>
        /// <param name="data"></param>
        public Packet(byte[] data)
        {
            Buffer = data;
            Length = data.Length;
            readPointer = 0;
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Copies the data from the given buffer into the local one.
        /// </summary>
        /// <param name="data">the data to copy.</param>
        /// <param name="length">the number of bytes to copy from the buffer.</param>
        public void SetTo(byte[] data, int length)
        {
            data.CopyTo(Buffer, 0);
            Length = length;
            readPointer = 0;
        }

        /// <summary>
        /// Resets this packet, setting length and read/write position to zero.
        /// </summary>
        public void Reset()
        {
            Length = 0;
            readPointer = 0;
        }

        /// <summary>
        /// Restart reading from the beginning.
        /// </summary>
        public void Rewind()
        {
            readPointer = 0;
        }

        #endregion

        #region Writing

        public void Write(bool data)
        {
            byte[] bytes = BitConverter.GetBytes(data);
            bytes.CopyTo(Buffer, Length);
            Length += bytes.Length;
        }

        public void Write(byte data)
        {
            Buffer[Length] = data;
            Length++;
        }

        public void Write(char data)
        {
            byte[] bytes = BitConverter.GetBytes(data);
            bytes.CopyTo(Buffer, Length);
            Length += bytes.Length;
        }

        public void Write(double data)
        {
            byte[] bytes = BitConverter.GetBytes(data);
            bytes.CopyTo(Buffer, Length);
            Length += bytes.Length;
        }

        public void Write(Fixed data)
        {
            byte[] bytes = BitConverter.GetBytes(data.RawValue);
            bytes.CopyTo(Buffer, Length);
            Length += bytes.Length;
        }

        public void Write(float data)
        {
            byte[] bytes = BitConverter.GetBytes(data);
            bytes.CopyTo(Buffer, Length);
            Length += bytes.Length;
        }

        public void Write(int data)
        {
            byte[] bytes = BitConverter.GetBytes(data);
            bytes.CopyTo(Buffer, Length);
            Length += bytes.Length;
        }

        public void Write(long data)
        {
            byte[] bytes = BitConverter.GetBytes(data);
            bytes.CopyTo(Buffer, Length);
            Length += bytes.Length;
        }

        public void Write(short data)
        {
            byte[] bytes = BitConverter.GetBytes(data);
            bytes.CopyTo(Buffer, Length);
            Length += bytes.Length;
        }

        public void Write(uint data)
        {
            byte[] bytes = BitConverter.GetBytes(data);
            bytes.CopyTo(Buffer, Length);
            Length += bytes.Length;
        }

        public void Write(ushort data)
        {
            byte[] bytes = BitConverter.GetBytes(data);
            bytes.CopyTo(Buffer, Length);
            Length += bytes.Length;
        }

        public void Write(ulong data)
        {
            byte[] bytes = BitConverter.GetBytes(data);
            bytes.CopyTo(Buffer, Length);
            Length += bytes.Length;
        }

        public void Write(byte[] data)
        {
            if (data.Length > ushort.MaxValue)
            {
                throw new ArgumentException("data");
            }
            Write(data, (ushort)data.Length);
        }

        public void Write(byte[] data, ushort length)
        {
            if (data == null)
            {
                Write((ushort)0);
            }
            else
            {
                Write(length);
                Array.Copy(data, 0, Buffer, this.Length, length);
                this.Length += length;
            }
        }

        public void Write(Packet data)
        {
            if (data == null)
            {
                Write((ushort)0);
            }
            else
            {
                if (data.Length > ushort.MaxValue)
                {
                    throw new ArgumentException("data");
                }
                Write(data.Buffer, (ushort)data.Length);
            }
        }

        public void Write(string data)
        {
            Write(Encoding.UTF8.GetBytes(data));
        }

        public void Write(FPoint data)
        {
            Write(data.X);
            Write(data.Y);
        }

        public void Write<TPacketizerContext>(IPacketizable<TPacketizerContext> data)
        {
            data.Packetize(this);
        }

        #endregion

        #region Reading

        public bool ReadBoolean()
        {
            if (!HasBoolean())
            {
                throw new PacketException("Cannot read boolean.");
            }
            var result = BitConverter.ToBoolean(Buffer, readPointer);
            readPointer += sizeof(bool);
            return result;
        }

        public byte ReadByte()
        {
            if (!HasByte())
            {
                throw new PacketException("Cannot read byte.");
            }
            var result = Buffer[readPointer];
            readPointer++;
            return result;
        }

        public char ReadChar()
        {
            if (!HasChar())
            {
                throw new PacketException("Cannot read char.");
            }
            var result = BitConverter.ToChar(Buffer, readPointer);
            readPointer += sizeof(char);
            return result;
        }

        public float ReadSingle()
        {
            if (!HasSingle())
            {
                throw new PacketException("Cannot read single.");
            }
            var result = BitConverter.ToSingle(Buffer, readPointer);
            readPointer += sizeof(float);
            return result;
        }

        public double ReadDouble()
        {
            if (!HasDouble())
            {
                throw new PacketException("Cannot read double.");
            }
            var result = BitConverter.ToDouble(Buffer, readPointer);
            readPointer += sizeof(double);
            return result;
        }

        public Fixed ReadFixed()
        {
            if (!HasFixed())
            {
                throw new PacketException("Cannot read fixed.");
            }
            var result = Fixed.Create(BitConverter.ToInt64(Buffer, readPointer), false);
            readPointer += sizeof(long);
            return result;
        }

        public short ReadInt16()
        {
            if (!HasInt16())
            {
                throw new PacketException("Cannot read int16.");
            }
            var result = BitConverter.ToInt16(Buffer, readPointer);
            readPointer += sizeof(short);
            return result;
        }

        public int ReadInt32()
        {
            if (!HasInt32())
            {
                throw new PacketException("Cannot read int32.");
            }
            var result = BitConverter.ToInt32(Buffer, readPointer);
            readPointer += sizeof(int);
            return result;
        }

        public long ReadInt64()
        {
            if (!HasInt64())
            {
                throw new PacketException("Cannot read int64.");
            }
            var result = BitConverter.ToInt64(Buffer, readPointer);
            readPointer += sizeof(long);
            return result;
        }

        public ushort ReadUInt16()
        {
            if (!HasUInt16())
            {
                throw new PacketException("Cannot read uint16.");
            }
            var result = BitConverter.ToUInt16(Buffer, readPointer);
            readPointer += sizeof(ushort);
            return result;
        }

        public uint ReadUInt32()
        {
            if (!HasUInt32())
            {
                throw new PacketException("Cannot read uint32.");
            }
            var result = BitConverter.ToUInt32(Buffer, readPointer);
            readPointer += sizeof(uint);
            return result;
        }

        public ulong ReadUInt64()
        {
            if (!HasUInt64())
            {
                throw new PacketException("Cannot read uint64.");
            }
            var result = BitConverter.ToUInt64(Buffer, readPointer);
            readPointer += sizeof(ulong);
            return result;
        }

        public byte[] ReadByteArray()
        {
            if (!HasByteArray())
            {
                throw new PacketException("Cannot read byte[].");
            }
            ushort length = BitConverter.ToUInt16(Buffer, readPointer);
            readPointer += sizeof(ushort);
            byte[] result = new byte[length];
            Array.Copy(Buffer, readPointer, result, 0, length);
            readPointer += length;
            return result;
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

        public void ReadPacketizable<TPacketizerContext>(IPacketizable<TPacketizerContext> packetizable, TPacketizerContext context)
        {
            packetizable.Depacketize(this, context);
        }

        #endregion

        #region Peeking

        public bool PeekBoolean()
        {
            if (!HasBoolean())
            {
                throw new PacketException("Cannot read boolean.");
            }
            return BitConverter.ToBoolean(Buffer, readPointer);
        }

        public byte PeekByte()
        {
            if (!HasByte())
            {
                throw new PacketException("Cannot read byte.");
            }
            return Buffer[readPointer];
        }

        public char PeekChar()
        {
            if (!HasChar())
            {
                throw new PacketException("Cannot read char.");
            }
            return BitConverter.ToChar(Buffer, readPointer);
        }

        public float PeekSingle()
        {
            if (!HasSingle())
            {
                throw new PacketException("Cannot read single.");
            }
            return BitConverter.ToSingle(Buffer, readPointer);
        }

        public double PeekDouble()
        {
            if (!HasDouble())
            {
                throw new PacketException("Cannot read double.");
            }
            return BitConverter.ToDouble(Buffer, readPointer);
        }

        public Fixed PeekFixed()
        {
            if (!HasFixed())
            {
                throw new PacketException("Cannot read fixed.");
            }
            return Fixed.Create(BitConverter.ToInt64(Buffer, readPointer), false);
        }

        public short PeekInt16()
        {
            if (!HasInt16())
            {
                throw new PacketException("Cannot read int16.");
            }
            return BitConverter.ToInt16(Buffer, readPointer);
        }

        public int PeekInt32()
        {
            if (!HasInt32())
            {
                throw new PacketException("Cannot read int32.");
            }
            return BitConverter.ToInt32(Buffer, readPointer);
        }

        public long PeekInt64()
        {
            if (!HasInt64())
            {
                throw new PacketException("Cannot read int64.");
            }
            return BitConverter.ToInt64(Buffer, readPointer);
        }

        public ushort PeekUInt16()
        {
            if (!HasUInt16())
            {
                throw new PacketException("Cannot read uint16.");
            }
            return BitConverter.ToUInt16(Buffer, readPointer);
        }

        public uint PeekUInt32()
        {
            if (!HasUInt32())
            {
                throw new PacketException("Cannot read uint32.");
            }
            return BitConverter.ToUInt32(Buffer, readPointer);
        }

        public ulong PeekUInt64()
        {
            if (!HasUInt64())
            {
                throw new PacketException("Cannot read uint64.");
            }
            return BitConverter.ToUInt64(Buffer, readPointer);
        }

        public byte[] PeekByteArray()
        {
            if (!HasByteArray())
            {
                throw new PacketException("Cannot read byte[].");
            }
            ushort length = BitConverter.ToUInt16(Buffer, readPointer);
            byte[] result = new byte[length];
            Array.Copy(Buffer, readPointer + sizeof(ushort), result, 0, length);
            return result;
        }

        public Packet PeekPacket()
        {
            if (!HasPacket())
            {
                throw new PacketException("Cannot read packet.");
            }
            return new Packet(PeekByteArray());
        }

        public string PeekString()
        {
            if (!HasString())
            {
                throw new PacketException("Cannot read string.");
            }
            return Encoding.UTF8.GetString(PeekByteArray());
        }

        #endregion

        #region Checking

        public bool HasBoolean()
        {
            return Available >= 1;
        }

        public bool HasByte()
        {
            return Available >= 1;
        }

        public bool HasChar()
        {
            return Available >= 1;
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
