using Engine.Serialization;
using Microsoft.Xna.Framework;

namespace Engine.XnaExtensions
{
    /// <summary>
    /// Packet write and read methods for XNA types.
    /// </summary>
    public static class PacketXnaExtensions
    {
        /// <summary>
        /// Writes the specified vector value.
        /// </summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        public static Packet Write(this Packet packet, Vector2 data)
        {
            return packet.Write(data.X).Write(data.Y);
        }

        /// <summary>
        /// Writes the specified vector value.
        /// </summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        public static Packet Write(this Packet packet, Vector3 data)
        {
            return packet.Write(data.X).Write(data.Y).Write(data.Z);
        }

        /// <summary>
        /// Writes the specified rectangle value.
        /// </summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        public static Packet Write(this Packet packet, Rectangle data)
        {
            return packet.Write(data.X).Write(data.Y).Write(data.Width).Write(data.Height);
        }

        /// <summary>
        /// Reads a vector value.
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        public static Vector2 ReadVector2(this Packet packet)
        {
            Vector2 result;
            result.X = packet.ReadSingle();
            result.Y = packet.ReadSingle();
            return result;
        }

        /// <summary>
        /// Reads a vector value.
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        public static Vector3 ReadVector3(this Packet packet)
        {
            Vector3 result;
            result.X = packet.ReadSingle();
            result.Y = packet.ReadSingle();
            result.Z = packet.ReadSingle();
            return result;
        }

        /// <summary>
        /// Reads a rectangle value.
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        public static Rectangle ReadRectangle(this Packet packet)
        {
            Rectangle result;
            result.X = packet.ReadInt32();
            result.Y = packet.ReadInt32();
            result.Width = packet.ReadInt32();
            result.Height = packet.ReadInt32();
            return result;
        }
    }
}
