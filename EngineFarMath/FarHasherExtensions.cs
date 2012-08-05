using Engine.Serialization;

namespace Engine.FarMath
{
    /// <summary>
    /// Hashing helpers for far types.
    /// </summary>
    public static class FarHasherExtensions
    {
        /// <summary>
        /// Put the specified value to the data of which the hash
        /// gets computed.
        /// </summary>
        /// <param name="hasher">The hasher to use.</param>
        /// <param name="value">the data to add.</param>
        /// <returns>
        /// a reference to the hasher, for chaining.
        /// </returns>
        public static Hasher Put(this Hasher hasher, FarPosition value)
        {
            return hasher.Put(value.X).Put(value.Y);
        }

        /// <summary>
        /// Put the specified value to the data of which the hash
        /// gets computed.
        /// </summary>
        /// <param name="hasher">The hasher to use.</param>
        /// <param name="value">the data to add.</param>
        /// <returns>
        /// a reference to the hasher, for chaining.
        /// </returns>
        public static Hasher Put(this Hasher hasher, FarRectangle value)
        {
            return hasher.Put(value.X).Put(value.Y).Put(value.Width).Put(value.Height);
        }
    }
}
