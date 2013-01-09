using System.Diagnostics;

namespace Engine.Util
{
    /// <summary>Some utility methods for bitwise operations.</summary>
    public static class BitwiseMagic
    {
        /// <summary>Checks if a number is a power of two.</summary>
        /// <param name="x">The value to check.</param>
        /// <returns>
        ///     <c>true</c> if the specified value is a power of two; <c>false</c> otherwise.
        /// </returns>
        /// <remarks>
        ///     Source: <see Href="http://www.exploringbinary.com/ten-ways-to-check-if-an-integer-is-a-power-of-two-in-c/"/>.
        /// </remarks>
        public static bool IsPowerOfTwo(uint x)
        {
            return (x != 0) && ((x & (~x + 1)) == x);
        }

        /// <summary>Gets the next higher power of two for a given number</summary>
        /// <remarks>
        ///     If a power of two is given, the next higher one will be returned, not the given one. When zero is given, zero is
        ///     returned. Source: <see cref="http://jeffreystedfast.blogspot.com/2008/06/calculating-nearest-power-of-2.html"/>
        /// </remarks>
        /// <param name="i">The number to get the next higher power of two for.</param>
        /// <returns>The next higher power of two.</returns>
        public static uint GetNextHighestPowerOfTwo(uint i)
        {
            uint j, k;
            if ((j = i & 0xFFFF0000) == 0)
            {
                j = i;
            }
            if ((k = j & 0xFF00FF00) == 0)
            {
                k = j;
            }
            if ((j = k & 0xF0F0F0F0) == 0)
            {
                j = k;
            }
            if ((k = j & 0xCCCCCCCC) == 0)
            {
                k = j;
            }
            if ((j = k & 0xAAAAAAAA) == 0)
            {
                j = k;
            }
            return j << 1;
        }

        /// <summary>Pack two integer values into one ulong value.</summary>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <returns>The value resulting from packing two values.</returns>
        public static ulong Pack(int x, int y)
        {
            return ((ulong) x << 32) | (uint) y;
        }

        /// <summary>Unpacks a ulong value into two integer values (previously packed using the Pack() method).</summary>
        /// <param name="xy">The value containing the two values.</param>
        /// <param name="x">The unpacked first value.</param>
        /// <param name="y">The unpacked second value.</param>
        public static void Unpack(ulong xy, out int x, out int y)
        {
            x = (int) (xy >> 32);
            y = (int) (xy & 0xFFFFFFFF);
        }

        /// <summary>Pack two short values into one uint value.</summary>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <returns>The value resulting from packing two values.</returns>
        public static uint Pack(short x, short y)
        {
            return ((uint) x << 16) | (ushort) y;
        }

        /// <summary>Unpacks a uint value into two short values (previously packed using the Pack() method).</summary>
        /// <param name="xy">The value containing the two values.</param>
        /// <param name="x">The unpacked first value.</param>
        /// <param name="y">The unpacked second value.</param>
        public static void Unpack(uint xy, out short x, out short y)
        {
            x = (short) (xy >> 16);
            y = (short) (xy & 0xFFFF);
        }

        /// <summary>
        ///     Perform a modulus operation on a value, where the dividend is a power of two. Note that C# handles modulo
        ///     pretty well, so in most cases that'll be faster than using this function.
        /// </summary>
        /// <param name="n">The dividend.</param>
        /// <param name="m">
        ///     The divisor. This <em>must be a power of two!</em>
        /// </param>
        /// <returns>The remainder.</returns>
        public static int Mod(int n, int m)
        {
            Debug.Assert(IsPowerOfTwo((uint) m));
            return n & (m - 1);
        }
    }
}