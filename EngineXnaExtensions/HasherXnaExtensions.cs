using Engine.Serialization;
using Microsoft.Xna.Framework;

namespace Engine.XnaExtensions
{
    /// <summary>
    /// Extension methods for Xna types.
    /// </summary>
    public static class HasherXnaExtensions
    {
        /// <summary>
        /// Put the specified value to the data of which the hash
        /// gets computed.
        /// </summary>
        /// <param name="hasher">The hasher to use.</param>
        /// <param name="value">the data to add.</param>
        /// <returns>a reference to the hasher, for chaining.</returns>
        public static Hasher Put(this Hasher hasher, Color value)
        {
            return hasher.Put(value.PackedValue);
        }

        /// <summary>
        /// Put the specified value to the data of which the hash
        /// gets computed.
        /// </summary>
        /// <param name="hasher">The hasher to use.</param>
        /// <param name="value">the data to add.</param>
        /// <returns>a reference to the hasher, for chaining.</returns>
        public static Hasher Put(this Hasher hasher, Vector2 value)
        {
            return hasher.Put(value.X).Put(value.Y);
        }

        /// <summary>
        /// Put the specified value to the data of which the hash
        /// gets computed.
        /// </summary>
        /// <param name="hasher">The hasher to use.</param>
        /// <param name="value">the data to add.</param>
        /// <returns>a reference to the hasher, for chaining.</returns>
        public static Hasher Put(this Hasher hasher, Vector3 value)
        {
            return hasher.Put(value.X).Put(value.Y).Put(value.Z);
        }

        /// <summary>
        /// Put the specified value to the data of which the hash
        /// gets computed.
        /// </summary>
        /// <param name="hasher">The hasher to use.</param>
        /// <param name="value">the data to add.</param>
        /// <returns>a reference to the hasher, for chaining.</returns>
        public static Hasher Put(this Hasher hasher, Matrix value)
        {
            return hasher.Put(value.M11).Put(value.M12).Put(value.M13).Put(value.M14).
                Put(value.M21).Put(value.M22).Put(value.M23).Put(value.M24).
                Put(value.M31).Put(value.M32).Put(value.M33).Put(value.M34).
                Put(value.M41).Put(value.M42).Put(value.M43).Put(value.M44);
        }

        /// <summary>
        /// Put the specified value to the data of which the hash
        /// gets computed.
        /// </summary>
        /// <param name="hasher">The hasher to use.</param>
        /// <param name="value">the data to add.</param>
        /// <returns>a reference to the hasher, for chaining.</returns>
        public static Hasher Put(this Hasher hasher, Rectangle value)
        {
            return hasher.Put(value.X).Put(value.Y).Put(value.Width).Put(value.Height);
        }
    }
}
