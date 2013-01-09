using System;
using System.Text;

namespace Engine.Serialization
{
    /// <summary>Common utility implementations for readable packets.</summary>
    public static class ReadablePacketExtensions
    {
        /// <summary>Reads a boolean value.</summary>
        /// <param name="packet">The packet.</param>
        /// <param name="data">The read value.</param>
        /// <returns>This packet, for call chaining.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public static IReadablePacket Read(this IReadablePacket packet, out bool data)
        {
            data = packet.ReadBoolean();
            return packet;
        }

        /// <summary>Reads a byte value.</summary>
        /// <param name="packet">The packet.</param>
        /// <param name="data">The read value.</param>
        /// <returns>This packet, for call chaining.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public static IReadablePacket Read(this IReadablePacket packet, out byte data)
        {
            data = packet.ReadByte();
            return packet;
        }

        /// <summary>Reads a single value.</summary>
        /// <param name="packet">The packet.</param>
        /// <param name="data">The read value.</param>
        /// <returns>This packet, for call chaining.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public static IReadablePacket Read(this IReadablePacket packet, out float data)
        {
            data = packet.ReadSingle();
            return packet;
        }

        /// <summary>Reads a double value.</summary>
        /// <param name="packet">The packet.</param>
        /// <param name="data">The read value.</param>
        /// <returns>This packet, for call chaining.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public static IReadablePacket Read(this IReadablePacket packet, out double data)
        {
            data = packet.ReadDouble();
            return packet;
        }

        /// <summary>Reads an int16 value.</summary>
        /// <param name="packet">The packet.</param>
        /// <param name="data">The read value.</param>
        /// <returns>This packet, for call chaining.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public static IReadablePacket Read(this IReadablePacket packet, out short data)
        {
            data = packet.ReadInt16();
            return packet;
        }

        /// <summary>Reads an int32 value.</summary>
        /// <param name="packet">The packet.</param>
        /// <param name="data">The read value.</param>
        /// <returns>This packet, for call chaining.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public static IReadablePacket Read(this IReadablePacket packet, out int data)
        {
            data = packet.ReadInt32();
            return packet;
        }

        /// <summary>Reads an int64 value.</summary>
        /// <param name="packet">The packet.</param>
        /// <param name="data">The read value.</param>
        /// <returns>This packet, for call chaining.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public static IReadablePacket Read(this IReadablePacket packet, out long data)
        {
            data = packet.ReadInt64();
            return packet;
        }

        /// <summary>Reads a uint16 value.</summary>
        /// <param name="packet">The packet.</param>
        /// <param name="data">The read value.</param>
        /// <returns>This packet, for call chaining.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public static IReadablePacket Read(this IReadablePacket packet, out ushort data)
        {
            data = packet.ReadUInt16();
            return packet;
        }

        /// <summary>Reads a uint32 value.</summary>
        /// <param name="packet">The packet.</param>
        /// <param name="data">The read value.</param>
        /// <returns>This packet, for call chaining.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public static IReadablePacket Read(this IReadablePacket packet, out uint data)
        {
            data = packet.ReadUInt32();
            return packet;
        }

        /// <summary>Reads a uint64 value.</summary>
        /// <param name="packet">The packet.</param>
        /// <param name="data">The read value.</param>
        /// <returns>This packet, for call chaining.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public static IReadablePacket Read(this IReadablePacket packet, out ulong data)
        {
            data = packet.ReadUInt64();
            return packet;
        }

        /// <summary>Reads a byte array.</summary>
        /// <param name="packet">The packet.</param>
        /// <param name="buffer">The buffer to write to.</param>
        /// <param name="offset">The offset to start writing at.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <param name="length">The number of bytes read.</param>
        /// <returns>This packet, for call chaining.</returns>
        public static IReadablePacket Read(
            this IReadablePacket packet, byte[] buffer, int offset, int count, out int length)
        {
            length = packet.ReadByteArray(buffer, offset, count);
            return packet;
        }

        /// <summary>
        ///     Reads a byte array.
        ///     <para>
        ///         May yield <c>null</c>.
        ///     </para>
        /// </summary>
        /// <param name="packet">The packet.</param>
        /// <param name="data">The read value.</param>
        /// <returns>This packet, for call chaining.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public static IReadablePacket Read(this IReadablePacket packet, out byte[] data)
        {
            data = packet.ReadByteArray();
            return packet;
        }

        /// <summary>
        ///     Reads a packet.
        ///     <para>
        ///         May yield <c>null</c>.
        ///     </para>
        /// </summary>
        /// <param name="packet">The packet.</param>
        /// <param name="data">The read value.</param>
        /// <returns>This packet, for call chaining.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public static IReadablePacket Read(this IReadablePacket packet, out IReadablePacket data)
        {
            data = packet.ReadPacket();
            return packet;
        }

        /// <summary>Reads a string value using UTF8 encoding.</summary>
        /// <param name="packet">The packet.</param>
        /// <param name="data">The read value</param>
        /// <returns>This packet, for call chaining.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public static IReadablePacket Read(this IReadablePacket packet, out string data)
        {
            data = packet.ReadString();
            return packet;
        }

        /// <summary>Reads a type value using its assembly qualified name for lookup.</summary>
        /// <param name="packet">The packet.</param>
        /// <param name="data">The read value.</param>
        /// <returns>This packet, for call chaining.</returns>
        /// <exception cref="PacketException">The type is not known in the local assembly.</exception>
        public static IReadablePacket Read(this IReadablePacket packet, out Type data)
        {
            data = packet.ReadType();
            return packet;
        }

        /// <summary>
        ///     Reads a packet.
        ///     <para>
        ///         May return <c>null</c>.
        ///     </para>
        /// </summary>
        /// <param name="packet">The packet.</param>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public static IReadablePacket ReadPacket(this IReadablePacket packet)
        {
            return (Packet) packet.ReadByteArray();
        }

        /// <summary>Reads a string value using UTF8 encoding.</summary>
        /// <param name="packet">The packet.</param>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public static string ReadString(this IReadablePacket packet)
        {
            if (!packet.HasString())
            {
                throw new PacketException("Cannot read string.");
            }
            var data = packet.ReadByteArray();
            return data == null ? null : Encoding.UTF8.GetString(data);
        }

        /// <summary>Reads a type value using its assembly qualified name for lookup.</summary>
        /// <param name="packet">The packet.</param>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The type is not known in the local assembly.</exception>
        public static Type ReadType(this IReadablePacket packet)
        {
            var typeName = packet.ReadString();
            if (typeName == null)
            {
                return null;
            }
            var type = Type.GetType(typeName);
            if (type == null)
            {
                throw new PacketException("Cannot read unknown Type ('" + typeName + "').");
            }
            return type;
        }

        /// <summary>
        ///     Reads an object collections.
        ///     <para>
        ///         May return <c>null</c>.
        ///     </para>
        /// </summary>
        /// <typeparam name="T">The type of the objects to read.</typeparam>
        /// <param name="packet">The packet.</param>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public static T[] ReadPacketizables<T>(this IReadablePacket packet)
            where T : class, IPacketizable, new()
        {
            return packet.ReadPacketizables(Packetizable.ReadPacketizable<T>);
        }

        /// <summary>
        ///     Reads an object collections.
        ///     <para>
        ///         May return <c>null</c>.
        ///     </para>
        /// </summary>
        /// <typeparam name="T">Basetype of the type of the objects actually being read.</typeparam>
        /// <param name="packet">The packet.</param>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public static T[] ReadPacketizablesWithTypeInfo<T>(this IReadablePacket packet)
            where T : class, IPacketizable
        {
            return packet.ReadPacketizables(Packetizable.ReadPacketizableWithTypeInfo<T>);
        }

        /// <summary>Internal method used to read object collections, using the specified method to read single objects.</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="packet">The packet.</param>
        /// <param name="reader">The reader.</param>
        /// <returns></returns>
        private static T[] ReadPacketizables<T>(this IReadablePacket packet, Func<IReadablePacket, T> reader)
            where T : IPacketizable
        {
            var packetizableCount = packet.ReadInt32();
            if (packetizableCount < 0)
            {
                return null;
            }

            var result = new T[packetizableCount];
            for (var i = 0; i < packetizableCount; i++)
            {
                result[i] = reader(packet);
            }
            return result;
        }
    }
}