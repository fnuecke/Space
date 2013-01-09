using System;

namespace Engine.Serialization
{
    /// <summary>
    ///     Implements the modified FNV hash as seen here: http://bretm.home.comcast.net/~bretm/hash/6.html
    ///     <para/>
    ///     This implementation allows hashing of an arbitrary format of data, one just has to keep calling one of the 'Write'
    ///     variants. A snapshot of the current hash can always be obtained via the <see cref="Value"/> property.
    /// </summary>
    public sealed class Hasher : IWritablePacket
    {
        #region Properties

        /// <summary>
        ///     Current value of the hash, based on the data given thus far. This performs some postprocessing, so keep a copy
        ///     if you reuse this a lot.
        /// </summary>
        public uint Value
        {
            get
            {
                unchecked
                {
                    var result = _hash;
                    result += result << 13;
                    result ^= result >> 7;
                    result += result << 3;
                    result ^= result >> 17;
                    result += result << 5;
                    return result;
                }
            }
        }

        /// <summary>The number of used bytes in the buffer.</summary>
        public int Length
        {
            get { return 0; }
        }

        #endregion

        #region Fields

        /// <summary>Multiplicand for single data.</summary>
        private const int P = 16777619;

        /// <summary>Current working value of the hash.</summary>
        private uint _hash;

        #endregion

        #region Constructor

        /// <summary>Creates a new hasher and initializes it.</summary>
        public Hasher()
        {
            Reset();
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose() {}

        #endregion

        #region Buffer

        /// <summary>
        ///     Returns the underlying array buffer of this packet. This is a reference to the actually used buffer, so it
        ///     should be treated as read-only.
        /// </summary>
        /// <returns>
        ///     The raw contents of this packet as a <c>byte[]</c>.
        /// </returns>
        public byte[] GetBuffer()
        {
            return null;
        }

        /// <summary>Reset this hasher to allow reusing it.</summary>
        public void Reset()
        {
            _hash = 2166136261;
        }

        #endregion

        #region Writing

        /// <summary>Put the specified value to the data of which the hash gets computed.</summary>
        /// <param name="value">the data to add.</param>
        /// <returns>a reference to the hasher, for chaining.</returns>
        public IWritablePacket Write(bool value)
        {
            return Write(BitConverter.GetBytes(value));
        }

        /// <summary>Put a single byte to the data of which the hash gets computed.</summary>
        /// <param name="value">the data to add.</param>
        /// <returns>a reference to the hasher, for chaining.</returns>
        public IWritablePacket Write(byte value)
        {
            unchecked
            {
                _hash = (_hash ^ value) * P;
            }
            return this;
        }

        /// <summary>Put the specified value to the data of which the hash gets computed.</summary>
        /// <param name="value">the data to add.</param>
        /// <returns>a reference to the hasher, for chaining.</returns>
        public IWritablePacket Write(double value)
        {
            return Write(BitConverter.GetBytes(value));
        }

        /// <summary>Put the specified value to the data of which the hash gets computed.</summary>
        /// <param name="value">the data to add.</param>
        /// <returns>a reference to the hasher, for chaining.</returns>
        public IWritablePacket Write(float value)
        {
            return Write(BitConverter.GetBytes(value));
        }

        /// <summary>Put the specified value to the data of which the hash gets computed.</summary>
        /// <param name="value">the data to add.</param>
        /// <returns>a reference to the hasher, for chaining.</returns>
        public IWritablePacket Write(int value)
        {
            return Write(BitConverter.GetBytes(value));
        }

        /// <summary>Put the specified value to the data of which the hash gets computed.</summary>
        /// <param name="value">the data to add.</param>
        /// <returns>a reference to the hasher, for chaining.</returns>
        public IWritablePacket Write(long value)
        {
            return Write(BitConverter.GetBytes(value));
        }

        /// <summary>Put the specified value to the data of which the hash gets computed.</summary>
        /// <param name="value">the data to add.</param>
        /// <returns>a reference to the hasher, for chaining.</returns>
        public IWritablePacket Write(short value)
        {
            return Write(BitConverter.GetBytes(value));
        }

        /// <summary>Put the specified value to the data of which the hash gets computed.</summary>
        /// <param name="value">the data to add.</param>
        /// <returns>a reference to the hasher, for chaining.</returns>
        public IWritablePacket Write(uint value)
        {
            return Write(BitConverter.GetBytes(value));
        }

        /// <summary>Put the specified value to the data of which the hash gets computed.</summary>
        /// <param name="value">the data to add.</param>
        /// <returns>a reference to the hasher, for chaining.</returns>
        public IWritablePacket Write(ulong value)
        {
            return Write(BitConverter.GetBytes(value));
        }

        /// <summary>Put the specified value to the data of which the hash gets computed.</summary>
        /// <param name="value">the data to add.</param>
        /// <returns>a reference to the hasher, for chaining.</returns>
        public IWritablePacket Write(ushort value)
        {
            return Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        ///     Writes the specified length from the specified byte array.
        ///     <para>
        ///         May be <c>null</c>.
        ///     </para>
        /// </summary>
        /// <param name="data">The value to write.</param>
        /// <param name="offset">The offset at which to start reading.</param>
        /// <param name="length">The number of bytes to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        public IWritablePacket Write(byte[] data, int offset, int length)
        {
            if (data == null)
            {
                return Write(-1);
            }

            Write(length);
            for (var i = offset; i < length; i++)
            {
                Write(data[i]);
            }
            return this;
        }

        /// <summary>
        ///     Internal method for writing byte arrays without the array's length. This is used to push bytified basic value
        ///     types (int, long, ...).
        /// </summary>
        /// <param name="data">The bytes to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        private IWritablePacket Write(byte[] data)
        {
            foreach (var b in data) {
                Write(b);
            }
            return this;
        }

        #endregion
    }
}