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
        /// Writes the specified matrix value.
        /// </summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        public static Packet Write(this Packet packet, Matrix data)
        {
            return packet.Write(data.M11).Write(data.M12).Write(data.M13).Write(data.M14).
                Write(data.M21).Write(data.M22).Write(data.M23).Write(data.M24).
                Write(data.M31).Write(data.M32).Write(data.M33).Write(data.M34).
                Write(data.M41).Write(data.M42).Write(data.M43).Write(data.M44);
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
        /// Writes the specified vector value.
        /// </summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        public static Packet Write(this Packet packet, Color data)
        {
            return packet.Write(data.PackedValue);
        }

        /// <summary>
        /// Reads a vector value.
        /// </summary>
        /// <param name="result">The read value.</param>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        /// <returns>This packet, for call chaining.</returns>
        public static Packet Read(this Packet packet, out Vector2 result)
        {
            result = packet.ReadVector2();
            return packet;
        }

        /// <summary>
        /// Reads a vector value.
        /// </summary>
        /// <param name="result">The read value.</param>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        /// <returns>This packet, for call chaining.</returns>
        public static Packet Read(this Packet packet, out Vector3 result)
        {
            result = packet.ReadVector3();
            return packet;
        }

        /// <summary>
        /// Reads a matrix value.
        /// </summary>
        /// <param name="result">The read value.</param>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        /// <returns>This packet, for call chaining.</returns>
        public static Packet Read(this Packet packet, out Matrix result)
        {
            result = packet.ReadMatrix();
            return packet;
        }

        /// <summary>
        /// Reads a rectangle value.
        /// </summary>
        /// <param name="result">The read value.</param>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        /// <returns>This packet, for call chaining.</returns>
        public static Packet Read(this Packet packet, out Rectangle result)
        {
            result = packet.ReadRectangle();
            return packet;
        }

        /// <summary>
        /// Reads a vector value.
        /// </summary>
        /// <param name="result">The read value.</param>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        /// <returns>This packet, for call chaining.</returns>
        public static Packet Read(this Packet packet, out Color result)
        {
            result = packet.ReadColor();
            return packet;
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
        /// Reads a matrix value.
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        public static Matrix ReadMatrix(this Packet packet)
        {
            Matrix result;
            result.M11 = packet.ReadSingle();
            result.M12 = packet.ReadSingle();
            result.M13 = packet.ReadSingle();
            result.M14 = packet.ReadSingle();
            result.M21 = packet.ReadSingle();
            result.M22 = packet.ReadSingle();
            result.M23 = packet.ReadSingle();
            result.M24 = packet.ReadSingle();
            result.M31 = packet.ReadSingle();
            result.M32 = packet.ReadSingle();
            result.M33 = packet.ReadSingle();
            result.M34 = packet.ReadSingle();
            result.M41 = packet.ReadSingle();
            result.M42 = packet.ReadSingle();
            result.M43 = packet.ReadSingle();
            result.M44 = packet.ReadSingle();
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

        /// <summary>
        /// Reads a vector value.
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        public static Color ReadColor(this Packet packet)
        {
            return new Color {PackedValue = packet.ReadUInt32()};
        }
    }
}
