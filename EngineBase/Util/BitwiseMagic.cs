namespace Engine.Util
{
    /// <summary>
    /// Some utility methods for bitwise operations.
    /// </summary>
    public static class BitwiseMagic
    {
        /// <summary>
        /// Gets the next higher power of two for a given number
        /// </summary>
        /// <remarks>
        /// If a power of two is given, the next higher one will be returned,
        /// not the given one. When zero is given, zero is returned.
        /// </remarks>
        /// <see cref="http://jeffreystedfast.blogspot.com/2008/06/calculating-nearest-power-of-2.html"/>
        /// <param name="i">The number to get the next higher power of two
        /// for.</param>
        /// <returns>The next higher power of two.</returns>
        public static uint GetNextHighestPowerOfTwo(uint i)
        {
            uint j, k;
            if ((j = i & 0xFFFF0000) == 0) j = i;
            if ((k = j & 0xFF00FF00) == 0) k = j;
            if ((j = k & 0xF0F0F0F0) == 0) j = k;
            if ((k = j & 0xCCCCCCCC) == 0) k = j;
            if ((j = k & 0xAAAAAAAA) == 0) j = k;
            return j << 1;
        }

        /// <summary>
        /// Pack two integer values into one ulong value.
        /// </summary>
        /// <param name="x">The first integer value.</param>
        /// <param name="y">The second integer value.</param>
        /// <returns>The long resulting from packing two values.</returns>
        public static ulong Pack(int x, int y)
        {
            return ((ulong)x << 32) | (uint)y;
        }

        /// <summary>
        /// Unpacks a ulong value into two integer values (previously packed
        /// using the Pack() method).
        /// </summary>
        /// <param name="xy">The ulong value containing the two integer values.</param>
        /// <param name="x">The unpacked first integer value.</param>
        /// <param name="y">The unpacked second integer value.</param>
        public static void Unpack(ulong xy, out int x, out int y)
        {
            x = (int)(xy >> 32);
            y = (int)(xy & 0xFFFFFFFF);
        }
    }
}
