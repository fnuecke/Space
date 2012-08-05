using Engine.Serialization;

namespace Engine.Math
{
    public static class RectangleFPacketExtensions
    {
        /// <summary>
        /// Writes the specified rectangle value.
        /// </summary>
        /// <param name="packet">The packet to write to.</param>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        public static Packet Write(this Packet packet, RectangleF data)
        {
            return packet.Write(data.X).Write(data.Y).Write(data.Width).Write(data.Height);
        }

        /// <summary>
        /// Reads a rectangle value.
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        public static RectangleF ReadRectangleF(this Packet packet)
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
