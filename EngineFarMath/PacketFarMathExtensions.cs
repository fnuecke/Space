using Engine.Serialization;

namespace Engine.FarMath
{
    /// <summary>
    /// Serialization helpers for far types.
    /// </summary>
    public static class PacketFarMathExtensions
    {
        /// <summary>
        /// Writes the specified far value.
        /// </summary>
        /// <param name="packet">The packet to write to.</param>
        /// <param name="data">The value to write.</param>
        /// <returns>
        /// This packet, for call chaining.
        /// </returns>
        public static IWritablePacket Write(this IWritablePacket packet, FarValue data)
        {
            return packet.Write(data.Segment).Write(data.Offset);
        }

        /// <summary>
        /// Writes the specified rectangle value.
        /// </summary>
        /// <param name="packet">The packet to write to.</param>
        /// <param name="data">The value to write.</param>
        /// <returns>
        /// This packet, for call chaining.
        /// </returns>
        public static IWritablePacket Write(this IWritablePacket packet, FarPosition data)
        {
            return packet.Write(data.X).Write(data.Y);
        }

        /// <summary>
        /// Writes the specified rectangle value.
        /// </summary>
        /// <param name="packet">The packet to write to.</param>
        /// <param name="data">The value to write.</param>
        /// <returns>
        /// This packet, for call chaining.
        /// </returns>
        public static IWritablePacket Write(this IWritablePacket packet, FarRectangle data)
        {
            return packet.Write(data.X).Write(data.Y).Write(data.Width).Write(data.Height);
        }

        /// <summary>
        /// Reads a far value.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        /// <param name="data">The read value.</param>
        /// <returns>
        /// This packet, for call chaining.
        /// </returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        public static IReadablePacket Read(this IReadablePacket packet, out FarValue data)
        {
            data = packet.ReadFarValue();
            return packet;
        }

        /// <summary>
        /// Reads a far position value.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        /// <param name="data">The read value.</param>
        /// <returns>
        /// This packet, for call chaining.
        /// </returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        public static IReadablePacket Read(this IReadablePacket packet, out FarPosition data)
        {
            data = packet.ReadFarPosition();
            return packet;
        }

        /// <summary>
        /// Reads a far rectangle value.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        /// <param name="data">The read value.</param>
        /// <returns>
        /// This packet, for call chaining.
        /// </returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        public static IReadablePacket Read(this IReadablePacket packet, out FarRectangle data)
        {
            data = packet.ReadFarRectangle();
            return packet;
        }

        /// <summary>
        /// Reads a far value.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        /// <returns>
        /// The read value.
        /// </returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        public static FarValue ReadFarValue(this IReadablePacket packet)
        {
            var segment = packet.ReadInt32();
            var offset = packet.ReadSingle();
            return new FarValue(segment, offset);
        }

        /// <summary>
        /// Reads a far position value.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        /// <returns>
        /// The read value.
        /// </returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        public static FarPosition ReadFarPosition(this IReadablePacket packet)
        {
            FarPosition result;
            result.X = packet.ReadFarValue();
            result.Y = packet.ReadFarValue();
            return result;
        }

        /// <summary>
        /// Reads a far rectangle value.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        /// <returns>
        /// The read value.
        /// </returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        public static FarRectangle ReadFarRectangle(this IReadablePacket packet)
        {
            FarRectangle result;
            result.X = packet.ReadFarValue();
            result.Y = packet.ReadFarValue();
            result.Width = packet.ReadSingle();
            result.Height = packet.ReadSingle();
            return result;
        }
    }
}
