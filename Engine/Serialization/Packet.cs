using System;
using System.Text;
using Engine.Math;

namespace Engine.Serialization
{
    public sealed class Packet
    {

        public int Available { get { return Length - readPointer; } }
        public byte[] Buffer { get; private set; }
        public int Length { get; private set; }

        private int readPointer;

        public Packet()
            : this(512)
        {
        }

        public Packet(long maxLength)
        {
            Buffer = new byte[maxLength];
            Reset();
        }

        public Packet(byte[] data)
        {
            Buffer = data;
            Length = data.Length;
            readPointer = 0;
        }

        public void SetTo(byte[] data, int length)
        {
            data.CopyTo(Buffer, 0);
            Length = length;
            readPointer = 0;
        }

        public void Reset()
        {
            Length = 0;
            readPointer = 0;
        }

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
            if (data == null)
            {
                Write((int)0);
            }
            else
            {
                byte[] bytes = BitConverter.GetBytes(data.Length);
                bytes.CopyTo(Buffer, Length);
                Length += bytes.Length;
                data.CopyTo(Buffer, Length);
                Length += data.Length;
            }
        }

        public void Write(Packet data)
        {
            if (data == null)
            {
                Write((int)0);
            }
            else
            {
                byte[] bytes = BitConverter.GetBytes(data.Length);
                bytes.CopyTo(Buffer, Length);
                Length += bytes.Length;
                Array.Copy(data.Buffer, 0, Buffer, Length, data.Length);
                Length += data.Length;
            }
        }

        public void Write(string data)
        {
            Write(Encoding.UTF8.GetBytes(data));
        }

        public bool ReadBoolean()
        {
            var result = BitConverter.ToBoolean(Buffer, readPointer);
            readPointer += sizeof(bool);
            return result;
        }

        public byte ReadByte()
        {
            var result = Buffer[readPointer];
            readPointer++;
            return result;
        }

        public char ReadChar()
        {
            var result = BitConverter.ToChar(Buffer, readPointer);
            readPointer += sizeof(char);
            return result;
        }

        public float ReadSingle()
        {
            var result = BitConverter.ToSingle(Buffer, readPointer);
            readPointer += sizeof(float);
            return result;
        }

        public double ReadDouble()
        {
            var result = BitConverter.ToDouble(Buffer, readPointer);
            readPointer += sizeof(double);
            return result;
        }

        public Fixed ReadFixed()
        {
            var result = Fixed.Create(BitConverter.ToInt64(Buffer, readPointer), false);
            readPointer += sizeof(long);
            return result;
        }

        public short ReadInt16()
        {
            var result = BitConverter.ToInt16(Buffer, readPointer);
            readPointer += sizeof(short);
            return result;
        }

        public int ReadInt32()
        {
            var result = BitConverter.ToInt32(Buffer, readPointer);
            readPointer += sizeof(int);
            return result;
        }

        public long ReadInt64()
        {
            var result = BitConverter.ToInt64(Buffer, readPointer);
            readPointer += sizeof(long);
            return result;
        }

        public ushort ReadUInt16()
        {
            var result = BitConverter.ToUInt16(Buffer, readPointer);
            readPointer += sizeof(ushort);
            return result;
        }

        public uint ReadUInt32()
        {
            var result = BitConverter.ToUInt32(Buffer, readPointer);
            readPointer += sizeof(uint);
            return result;
        }

        public ulong ReadUInt64()
        {
            var result = BitConverter.ToUInt64(Buffer, readPointer);
            readPointer += sizeof(ulong);
            return result;
        }

        public byte[] ReadByteArray()
        {
            int length = BitConverter.ToInt32(Buffer, readPointer);
            readPointer += sizeof(int);
            byte[] result = new byte[length];
            Array.Copy(Buffer, readPointer, result, 0, length);
            readPointer += length;
            return result;
        }

        public Packet ReadPacket()
        {
            return new Packet(ReadByteArray());
        }

        public string ReadString()
        {
            return Encoding.UTF8.GetString(ReadByteArray());
        }

        public bool PeekBoolean()
        {
            return BitConverter.ToBoolean(Buffer, readPointer);
        }

        public byte PeekByte()
        {
            return Buffer[readPointer];
        }

        public char PeekChar()
        {
            return BitConverter.ToChar(Buffer, readPointer);
        }

        public float PeekSingle()
        {
            return BitConverter.ToSingle(Buffer, readPointer);
        }

        public double PeekDouble()
        {
            return BitConverter.ToDouble(Buffer, readPointer);
        }

        public Fixed PeekFixed()
        {
            return Fixed.Create(BitConverter.ToInt64(Buffer, readPointer), false);
        }

        public short PeekInt16()
        {
            return BitConverter.ToInt16(Buffer, readPointer);
        }

        public int PeekInt32()
        {
            return BitConverter.ToInt32(Buffer, readPointer);
        }

        public long PeekInt64()
        {
            return BitConverter.ToInt64(Buffer, readPointer);
        }

        public ushort PeekUInt16()
        {
            return BitConverter.ToUInt16(Buffer, readPointer);
        }

        public uint PeekUInt32()
        {
            return BitConverter.ToUInt32(Buffer, readPointer);
        }

        public ulong PeekUInt64()
        {
            return BitConverter.ToUInt64(Buffer, readPointer);
        }

        public byte[] PeekByteArray()
        {
            int length = BitConverter.ToInt32(Buffer, readPointer);
            byte[] result = new byte[length];
            Array.Copy(Buffer, readPointer + sizeof(int), result, 0, length);
            return result;
        }

        public Packet PeekPacket()
        {
            return new Packet(PeekByteArray());
        }

        public string PeekString()
        {
            return Encoding.UTF8.GetString(PeekByteArray());
        }

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
            if (HasInt32())
            {
                return Available >= sizeof(int) + PeekInt32();
            }
            return false;
        }

        public bool HasPacket()
        {
            if (HasInt32())
            {
                return Available >= sizeof(int) + PeekInt32();
            }
            return false;
        }

        public bool HasString()
        {
            return HasByteArray();
        }

    }
}
