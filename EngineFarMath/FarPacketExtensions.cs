using Engine.Serialization;

namespace Engine.FarMath
{
    /// <summary>
    /// Serialization helpers for far types.
    /// </summary>
    public static class FarPacketExtensions
    {
        /// <summary>
        /// Writes the specified rectangle value.
        /// </summary>
        /// <param name="packet">The packet to write to.</param>
        /// <param name="data">The value to write.</param>
        /// <returns>
        /// This packet, for call chaining.
        /// </returns>
        public static Packet Write(this Packet packet, FarPosition data)
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
        public static Packet Write(this Packet packet, FarRectangle data)
        {
            return packet.Write(data.X).Write(data.Y).Write(data.Width).Write(data.Height);
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
        public static FarValue ReadFarValue(this Packet packet)
        {
            var result = new FarValue();
            packet.ReadPacketizableInto(ref result);
            return result;
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
        public static FarPosition ReadFarPosition(this Packet packet)
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
        public static FarRectangle ReadFarRectangle(this Packet packet)
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
