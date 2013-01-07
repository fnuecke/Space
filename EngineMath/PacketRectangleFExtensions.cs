using Engine.Serialization;

namespace Engine.Math
{
    /// <summary>
    /// Packet extensions for serializing floating point rectangles.
    /// </summary>
    public static class PacketRectangleFExtensions
    {
        /// <summary>Writes the specified rectangle value.</summary>
        /// <param name="packet">The packet to write to.</param>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        public static IWritablePacket Write(this IWritablePacket packet, RectangleF data)
        {
            return packet.Write(data.X).Write(data.Y).Write(data.Width).Write(data.Height);
        }

        /// <summary>Reads a rectangle value.</summary>
        /// <param name="packet">The packet.</param>
        /// <param name="result">The read value.</param>
        /// <returns>This packet, for call chaining.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        public static IReadablePacket Read(this IReadablePacket packet, out RectangleF result)
        {
            result = packet.ReadRectangleF();
            return packet;
        }

        /// <summary>Reads a rectangle value.</summary>
        /// <param name="packet">The packet.</param>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        public static RectangleF ReadRectangleF(this IReadablePacket packet)
        {
            RectangleF result;
            result.X = packet.ReadSingle();
            result.Y = packet.ReadSingle();
            result.Width = packet.ReadSingle();
            result.Height = packet.ReadSingle();
            return result;
        }
    }
}
