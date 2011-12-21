/* 
   Copyright (C) 1997 - 2002, Makoto Matsumoto and Takuji Nishimura,
   All rights reserved.                          

   Redistribution and use in source and binary forms, with or without
   modification, are permitted provided that the following conditions
   are met:

     1. Redistributions of source code must retain the above copyright
        notice, this list of conditions and the following disclaimer.

     2. Redistributions in binary form must reproduce the above copyright
        notice, this list of conditions and the following disclaimer in the
        documentation and/or other materials provided with the distribution.

     3. The names of its contributors may not be used to endorse or promote 
        products derived from this software without specific prior written 
        permission.

   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
   "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
   LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
   A PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE COPYRIGHT OWNER OR
   CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
   EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
   PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
   PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
   LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
   NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
   SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

   This C# porting is done by stlalv on October 8, 2010.
   e-mail:stlalv @ nifty.com (remove space)
   Documentation and adjustment of interface to mimic that of .Net's Random API by fnuecke, Dec 20 2011.
   Original C code is found at http://www.math.sci.hiroshima-u.ac.jp/~m-mat/MT/MT2002/emt19937ar.html as mt19937ar.tgz
*/

using System;
using Engine.Serialization;

namespace Engine.Util
{
    /// <summary>
    /// Pseudo-Random number using the Mersenne-Twister algorithm (MT19937 variant).
    /// </summary>
    public sealed class MersenneTwister : IPacketizable, ICloneable
    {
        #region Constants: period parameters

        /// <summary>
        /// Degree of recurrence.
        /// </summary>
        private const int N = 624;

        /// <summary>
        /// Middle word, or the number of parallel sequences.
        /// </summary>
        private const int M = 397;

        /// <summary>
        /// Constant vector A.
        /// </summary>
        private const ulong MATRIX_A = 0x9908b0dfUL;

        /// <summary>
        /// Most significant w-r bits.
        /// </summary>
        private const ulong UPPER_MASK = 0x80000000UL;

        /// <summary>
        /// Least significant r bits.
        /// </summary>
        private const ulong LOWER_MASK = 0x7fffffffUL;

        #endregion

        #region Fields
        
        /// <summary>
        /// The array for the state vector.
        /// </summary>
        private ulong[] mt = new ulong[N];

        /// <summary>
        /// 
        /// </summary>
        private int mti;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>MersenneTwister</c> class,
        /// using a time-dependent default seed value.
        /// </summary>
        public MersenneTwister()
            : this((ulong)Environment.TickCount)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>MersenneTwister</c> class,
        /// using the specified seed value.
        /// </summary>
        /// <param name="seed">A number used to calculate a starting value for
        /// the pseudo-random number sequence.</param>
        public MersenneTwister(ulong seed)
        {
            mt[0] = seed & 0xffffffffUL;
            for (mti = 1; mti < N; mti++)
            {
                mt[mti] = (1812433253UL * (mt[mti - 1] ^ (mt[mti - 1] >> 30)) + (ulong)mti);
                /* See Knuth TAOCP Vol2. 3rd Ed. P.106 for multiplier. */
                /* In the previous versions, MSBs of the seed affect   */
                /* only MSBs of the array mt[].                        */
                /* 2002/01/09 modified by Makoto Matsumoto             */
                mt[mti] &= 0xffffffffUL;
                /* for >32 bit machines */
            }
        }
        
        #endregion

        #region Public API

        /// <summary>
        /// Returns a random number between 0.0 and 1.0, i.e. [0.0, 1.0).
        /// </summary>
        /// <returns>A double-precision floating point number greater than or
        /// equal to 0.0, and less than 1.0.</returns>
        public double NextDouble()
        {
            return Sample() * (1.0 / 4294967296.0);
        }

        /// <summary>
        /// Returns a nonnegative random number.
        /// </summary>
        /// <returns>A 32-bit signed integer greater than or equal to zero and
        /// less than <see cref="MaxValue"/>.</returns>
        public int Next()
        {
            return Next(int.MaxValue);
        }

        /// <summary>
        /// Returns a nonnegative random number less than the specified maximum.
        /// </summary>
        /// <param name="maxValue">The exclusive upper bound of the random
        /// number to be generated. maxValue must be greater than or equal to
        /// zero.</param>
        /// <returns>A 32-bit signed integer greater than or equal to zero, and
        /// less than maxValue; that is, the range of return values ordinarily
        /// includes zero but not maxValue. However, if maxValue equals zero,
        /// maxValue is returned.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><em>maxValue</em> is
        /// less than zero. </exception>
        public int Next(int maxValue)
        {
            if (maxValue < 0)
            {
                throw new ArgumentOutOfRangeException("maxValue");
            }
            return (int)(Sample() * (maxValue / 4294967296.0));
        }

        /// <summary>
        /// Returns a random number within a specified range.
        /// </summary>
        /// <param name="minValue">The inclusive lower bound of the random
        /// number returned.</param>
        /// <param name="maxValue">The exclusive upper bound of the random
        /// number returned. <em>maxValue</em> must be greater than or equal
        /// to <em>minValue</em>.</param>
        /// <returns>A 32-bit signed integer greater than or equal to
        /// <em>minValue</em> and less than <em>maxValue</em>; that is, the
        /// range of return values includes <em>minValue</em> but not
        /// <em>maxValue</em>. If <em>minValue</em> equals <em>maxValue</em>,
        /// <em>minValue</em> is returned.</returns>
        public int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
            {
                throw new ArgumentOutOfRangeException("minValue");
            }
            return minValue + Next(maxValue - minValue);
        }

        #endregion

        #region Internals

        /// <summary>
        /// Generates a random number on [0,0xffffffff]-interval.
        /// </summary>
        /// <returns></returns>
        private ulong Sample()
        {
            ulong[] mag01 = new ulong[] { 0x0UL, MATRIX_A };
            ulong y = 0;
            // mag01[x] = x * MATRIX_A  for x=0,1
            if (mti == N)
            {
                // generate N words at one time
                int kk;
                for (kk = 0; kk < N - M; kk++)
                {
                    y = (mt[kk] & UPPER_MASK) | (mt[kk + 1] & LOWER_MASK);
                    mt[kk] = mt[kk + M] ^ (y >> 1) ^ mag01[y & 0x1UL];
                }
                for (; kk < N - 1; kk++)
                {
                    y = (mt[kk] & UPPER_MASK) | (mt[kk + 1] & LOWER_MASK);
                    mt[kk] = mt[kk + (M - N)] ^ (y >> 1) ^ mag01[y & 0x1UL];
                }
                y = (mt[N - 1] & UPPER_MASK) | (mt[0] & LOWER_MASK);
                mt[N - 1] = mt[M - 1] ^ (y >> 1) ^ mag01[y & 0x1UL];
                mti = 0;
            }
            y = mt[mti++];
            // Tempering
            y ^= (y >> 11);
            y ^= (y << 7) & 0x9d2c5680UL;
            y ^= (y << 15) & 0xefc60000UL;
            y ^= (y >> 18);
            return y;
        }

        #endregion

        #region Serializing / Cloning

        public Packet Packetize(Packet packet)
        {
            for (int i = 0; i < N; i++)
            {
                packet.Write(mt[i]);
            }
            return packet.Write(mti);
        }

        public void Depacketize(Packet packet)
        {
            for (int i = 0; i < N; i++)
            {
                mt[i] = packet.ReadUInt64();
            }
            mti = packet.ReadInt32();
        }

        public object Clone()
        {
            var copy = (MersenneTwister)MemberwiseClone();
            copy.mt = new ulong[N];
            mt.CopyTo(copy.mt, 0);
            return copy;
        }

        #endregion
    }
}