using System;

namespace Engine.Util
{
    /// <summary>
    /// Implements the modified FNV hash as seen here:
    /// http://bretm.home.comcast.net/~bretm/hash/6.html
    /// 
    /// This implementation allows hashing of an arbitrary format
    /// of data, one just has to keep calling one of the <c>Put()</c>
    /// variants.
    /// 
    /// A snapshot of the current hash can always be obtained via
    /// the <c>Value</c> property.
    /// </summary>
    public sealed class Hasher
    {
        #region Properties
        
        /// <summary>
        /// Current value of the hash, based on the data given thus
        /// far. This performs some postprocessing, so keep a copy
        /// if you reuse this a lot.
        /// </summary>
        public int Value
        {
            get
            {
                unchecked
                {
                    int result = hash;
                    result += result << 13;
                    result ^= result >> 7;
                    result += result << 3;
                    result ^= result >> 17;
                    result += result << 5;
                    return result;
                }
            }
        }

        #endregion

        #region Fields
        
        /// <summary>
        /// Multiplicator for single data.
        /// </summary>
        private const int p = 16777619;

        /// <summary>
        /// Current working value of the hash.
        /// </summary>
        private int hash;

        #endregion

        /// <summary>
        /// Creates a new hasher and initializes it.
        /// </summary>
        public Hasher()
        {
            unchecked
            {
                hash = (int)2166136261;
            }
        }

        /// <summary>
        /// Put a single byte to the data of which the hash
        /// gets computed.
        /// </summary>
        /// <param name="value">the data to add.</param>
        public void Put(byte value)
        {
            unchecked
            {
                hash = (hash ^ value) * p;
            }
        }

        /// <summary>
        /// Put a byte arry to the data of which the hash
        /// gets computed.
        /// </summary>
        /// <param name="value">the data to add.</param>
        public void Put(byte[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            for (int i = 0; i < value.Length; i++)
            {
                Put(value[i]);
            }
        }
    }
}
