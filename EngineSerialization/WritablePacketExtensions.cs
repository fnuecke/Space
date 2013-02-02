using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;

namespace Engine.Serialization
{
    /// <summary>Common utility implementations for writable packets.</summary>
    public static class WritablePacketExtensions
    {
        /// <summary>
        ///     Writes the specified byte array.
        ///     <para/>
        ///     May be <c>null</c>.
        /// </summary>
        /// <param name="packet">The packet.</param>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        [PublicAPI]
        public static IWritablePacket Write(this IWritablePacket packet, byte[] data)
        {
            return data == null ? packet.Write(-1) : packet.Write(data, 0, data.Length);
        }

        /// <summary>Writes the specified string value using UTF8 encoding.</summary>
        /// <param name="packet">The packet.</param>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        [PublicAPI]
        public static IWritablePacket Write(this IWritablePacket packet, string data)
        {
            return data == null ? packet.Write((byte[]) null) : packet.Write(Encoding.UTF8.GetBytes(data));
        }

        /// <summary>Writes the specified type using its assembly qualified name.</summary>
        /// <param name="packet">The packet.</param>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        [PublicAPI]
        public static IWritablePacket Write(this IWritablePacket packet, Type data)
        {
            return data == null ? packet.Write((string) null) : packet.Write(data.AssemblyQualifiedName);
        }

        /// <summary>
        ///     Writes the specified collection of objects.
        ///     <para/>
        ///     Must byte read back using <see cref="ReadablePacketExtensions.ReadPacketizables{T}"/>.
        ///     <para/>
        ///     May be <c>null</c>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="packet">The packet.</param>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        [PublicAPI]
        public static IWritablePacket Write<T>(this IWritablePacket packet, ICollection<T> data)
            where T : class
        {
            if (data == null)
            {
                return packet.Write(-1);
            }

            packet.Write(data.Count);
            foreach (var item in data)
            {
                packet.Write(item);
            }
            return packet;
        }

        /// <summary>
        ///     Writes the specified collection of objects.
        ///     <para/>
        ///     Must byte read back using <see cref="ReadablePacketExtensions.ReadPacketizablesWithTypeInfo{T}"/>.
        ///     <para/>
        ///     May be <c>null</c>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="packet">The packet.</param>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        [PublicAPI]
        public static IWritablePacket WriteWithTypeInfo<T>(this IWritablePacket packet, ICollection<T> data)
            where T : class
        {
            if (data == null)
            {
                return packet.Write(-1);
            }

            packet.Write(data.Count);
            foreach (var item in data)
            {
                packet.WriteWithTypeInfo(item);
            }
            return packet;
        }
    }
}