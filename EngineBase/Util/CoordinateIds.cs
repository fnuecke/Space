using System;

namespace Engine.Util
{
    /// <summary>
    /// Utility class to convert coordinates to composite coordinate ids and
    /// vice versa.
    /// </summary>
    public static class CoordinateIds
    {
        /// <summary>
        /// Combine two coordinates into one coordinate id.
        /// </summary>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        /// <returns>The id resulting from the two coordinates.</returns>
        public static ulong Combine(int x, int y)
        {
            return ((ulong)x << 32) | (uint)y;
        }

        /// <summary>
        /// Split a coordinate id into its two coordinate components.
        /// </summary>
        /// <param name="xy">The id representing the two ids.</param>
        /// <returns>A tuple containing the two, separate coordinates.</returns>
        public static Tuple<int, int> Split(ulong xy)
        {
            return Tuple.Create((int)(xy >> 32), (int)(xy & 0xFFFFFFFF));
        }
    }
}
