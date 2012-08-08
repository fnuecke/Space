using System;
using Engine.Serialization;

namespace Engine.FarMath
{
    /// <summary>
    /// A "far value", inspired by the concept of "far positions" as described in
    /// Game Programming Gems 4, page 157: Solving Accuracy Problems in Large
    /// World Coordinates by P. Freese.
    /// 
    /// <para>
    /// This abstracts the concept one more step, to actually represent one-
    /// dimensional values, instead of two/three dimensional ones. By supplying
    /// some basic operators this allows a very seamless integration in systems
    /// that otherwise might use normal floating point types.
    /// </para>
    /// </summary>
    public struct FarValue : IComparable<FarValue>, IEquatable<FarValue>, IPacketizable, IHashable
    {
        #region Constants

        /// <summary>
        /// Size of a single segment. 2^16 allows us to cover the area we can
        /// describe with an int32. Adjust as necessary for the context this
        /// is used in.
        /// </summary>
        public const int SegmentSize = (1 << 16);

        /// <summary>
        /// Represents the origin, equivalent to a floating point zero.
        /// </summary>
        public static FarValue Zero
        {
            get { return ConstZero; }
        }

        /// <summary>
        /// Keep as private field to avoid manipulation.
        /// </summary>
        private static readonly FarValue ConstZero = new FarValue(0);

        #endregion

        #region Properties

        /// <summary>
        /// Returns the segment of this <see cref="FarValue"/>.
        /// </summary>
        public int Segment { get { return _segment; } }

        #endregion

        #region Fields

        /// <summary>
        /// The segment of this value.
        /// </summary>
        private int _segment;

        /// <summary>
        /// The offset inside the current segment.
        /// </summary>
        private float _offset;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="FarValue"/> struct.
        /// </summary>
        /// <param name="value">The initial value.</param>
        public FarValue(float value)
        {
            _segment = 0;
            _offset = value;
            Normalize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FarValue"/> struct.
        /// </summary>
        /// <param name="value">The initial value.</param>
        public FarValue(int value)
        {
            _segment = value / SegmentSize;
            _offset = value % SegmentSize;
        }

        /// <summary>
        /// Normalizes this <see cref="FarValue"/> by ensuring the local offset
        /// is within bounds of the current segment. Note that this implementation
        /// differs from the one given in Game Programming Gems: it only allows
        /// the offset to fit into the very segment we're in. In Freese's sample
        /// code the normalization allowed the offset to also point into the
        /// below segment (as it was a vector, actually 3 other segments). This
        /// is because that implementation used an abs(offset) check, so negative
        /// values were treated the same as positive ones.
        /// </summary>
        private void Normalize()
        {
            if (_offset >= SegmentSize)
            {
                _segment += (int)(_offset / SegmentSize);
                _offset = _offset % SegmentSize;
            }
            else if (_offset < 0f)
            {
                // Round the result of the division to the next lowest value, to
                // make sure we get into the next lower segment...
                _segment += (int)Math.Floor(_offset / SegmentSize);
                // ... compensate this by adding one segment size to the remaining
                // offset, to make sure it's positive afterwards.
                _offset = _offset % SegmentSize;
                if (_offset < 0)
                {
                    _offset += SegmentSize;
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns the absolute value of the specified <see cref="FarValue"/>.
        /// </summary>
        /// <param name="value">The value to get the absolute value for.</param>
        /// <returns>The absolute value.</returns>
        public static FarValue Abs(FarValue value)
        {
            return value > 0f ? value : -value;
        }

        /// <summary>
        /// Returns the smaller of the two specified values.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>
        /// The smaller value.
        /// </returns>
        public static FarValue Min(FarValue value1, FarValue value2)
        {
            return (value1 < value2) ? value1 : value2;
        }

        /// <summary>
        /// Returns the smaller of the two specified values.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <param name="result">The smaller value.</param>
        public static void Min(ref FarValue value1, ref FarValue value2, out FarValue result)
        {
            result = (value1 < value2) ? value1 : value2;
        }

        /// <summary>
        /// Returns the larger of the two specified values.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>
        /// The larger value.
        /// </returns>
        public static FarValue Max(FarValue value1, FarValue value2)
        {
            return (value1 > value2) ? value1 : value2;
        }

        /// <summary>
        /// Returns the larger of the two specified values.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <param name="result">The larger value.</param>
        public static void Max(ref FarValue value1, ref FarValue value2, out FarValue result)
        {
            result = (value1 > value2) ? value1 : value2;
        }

        /// <summary>
        /// Clamps the specified value between the specified minimum and maximum.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The minimum of the interval to clamp to.</param>
        /// <param name="max">The maximum of the interval to clamp to.</param>
        /// <returns>The clamped value.</returns>
        public static FarValue Clamp(FarValue value, FarValue min, FarValue max)
        {
            if (min > max)
            {
                // Inlined: return Min(max, Max(min, value1));
                return value < max ? max : value > min ? min : value;
            }
            else
            {
                // Inlined: return Min(max, Max(min, value1));
                return value > max ? max : value < min ? min : value;
            }
        }

        /// <summary>
        /// Clamps the specified value between the specified minimum and maximum.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The minimum of the interval to clamp to.</param>
        /// <param name="max">The maximum of the interval to clamp to.</param>
        /// <param name="result">The clamped value.</param>
        public static void Clamp(ref FarValue value, ref FarValue min, ref FarValue max, out FarValue result)
        {
            if (min > max)
            {
                // Inlined: result = Min(max, Max(min, value));
                result = value < max ? max : value > min ? min : value;
            }
            else
            {
                // Inlined: result = Min(max, Max(min, value));
                result = value > max ? max : value < min ? min : value;
            }
        }

        /// <summary>
        /// Performs a linear interpolation between the specified values.
        /// </summary>
        /// <param name="value1">The value to interpolate from.</param>
        /// <param name="value2">The value to interpolate towards.</param>
        /// <param name="amount">The amount to interpolate.</param>
        /// <returns>
        /// The interpolated value.
        /// </returns>
        public static FarValue Lerp(FarValue value1, FarValue value2, float amount)
        {
            // Build new value at the interpolated value.
            return value1 + (value2 - value1) * amount;
        }

        /// <summary>
        /// Performs a linear interpolation between the specified values.
        /// </summary>
        /// <param name="value1">The value to interpolate from.</param>
        /// <param name="value2">The value to interpolate towards.</param>
        /// <param name="amount">The amount to interpolate.</param>
        /// <param name="result">The interpolated value.</param>
        public static void Lerp(ref FarValue value1, ref FarValue value2, float amount, out FarValue result)
        {
            // Build new value at the interpolated value.
            result = value1 + (value2 - value1) * amount;
        }

        /// <summary>
        /// Performs a <see href="https://en.wikipedia.org/wiki/Smoothstep"/>
        /// interpolation between the specified values.
        /// </summary>
        /// <param name="value1">The value to interpolate from.</param>
        /// <param name="value2">The value to interpolate towards.</param>
        /// <param name="amount">The amount to interpolate.</param>
        /// <returns>The interpolated value.</returns>
        public static FarValue SmoothStep(FarValue value1, FarValue value2, float amount)
        {
            // Clamp. Inlined: Max(0, Min(1, amount))
            amount = (amount > 1f) ? 1f : ((amount < 0f) ? 0f : amount);
            // Evaluate polynomial.
            amount = amount * amount * (3f - 2f * amount);
            // Scale and add.
            return value1 + (value2 - value1) * amount;

        }

        /// <summary>
        /// Performs a <see href="https://en.wikipedia.org/wiki/Smoothstep"/>
        /// interpolation between the specified values.
        /// </summary>
        /// <param name="value1">The value to interpolate from.</param>
        /// <param name="value2">The value to interpolate towards.</param>
        /// <param name="amount">The amount to interpolate.</param>
        /// <param name="result">The interpolated value.</param>
        public static void SmoothStep(ref FarValue value1, ref FarValue value2, float amount, out FarValue result)
        {
            // Clamp. Inlined: Max(0, Min(1, amount))
            amount = (amount > 1f) ? 1f : ((amount < 0f) ? 0f : amount);
            // Evaluate polynomial.
            amount = amount * amount * (3f - 2f * amount);
            // Scale and add.
            result = value1 + (value2 - value1) * amount;
        }

        #endregion

        #region Operators

        /// <summary>
        /// Returns the inverse of the specified value (i.e. a <see cref="FarValue"/> of the
        /// same absolute value but with the opposite sign).
        /// </summary>
        /// <param name="value">The value to invert.</param>
        /// <returns>
        /// The inverse value.
        /// </returns>
        public static FarValue operator -(FarValue value)
        {
            FarValue result;
            result._segment = -value._segment;
            result._offset = -value._offset;
            result.Normalize();
            return result;
        }

        /// <summary>
        /// Adds the two specified <see cref="FarValue"/>s together.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>
        /// The sum of the two values.
        /// </returns>
        public static FarValue operator +(FarValue value1, FarValue value2)
        {
            var result = value1;
            result._segment += value2._segment;
            result._offset += value2._offset;
            result.Normalize();
            return result;
        }

        /// <summary>
        /// Adds the specified <see cref="FarValue"/> and the specified <see cref="float"/> together.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>
        /// The sum of the two values.
        /// </returns>
        public static FarValue operator +(FarValue value1, float value2)
        {
            var result = value1;
            result._offset += value2;
            result.Normalize();
            return result;
        }

        /// <summary>
        /// Adds the specified <see cref="FarValue"/> and the specified <see cref="float"/> together.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>
        /// The sum of the two values.
        /// </returns>
        public static FarValue operator +(float value1, FarValue value2)
        {
            var result = value2;
            result._offset += value1;
            result.Normalize();
            return result;
        }

        /// <summary>
        /// Adds the specified <see cref="FarValue"/> and the specified <see cref="int"/> together.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>
        /// The sum of the two values.
        /// </returns>
        public static FarValue operator +(FarValue value1, int value2)
        {
            var result = value1;
            result._segment += value2 / SegmentSize;
            result._offset += value2 % SegmentSize;
            return result;
        }

        /// <summary>
        /// Adds the specified <see cref="FarValue"/> and the specified <see cref="int"/> together.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>
        /// The sum of the two values.
        /// </returns>
        public static FarValue operator +(int value1, FarValue value2)
        {
            var result = value2;
            result._segment += value1 / SegmentSize;
            result._offset += value1 % SegmentSize;
            return result;
        }

        /// <summary>
        /// Subtracts the specified second <see cref="FarValue"/> value from the first specified <see cref="FarValue"/>.
        /// </summary>
        /// <param name="value1">The value to subtract from.</param>
        /// <param name="value2">The value to subtract.</param>
        /// <returns>
        /// The result of the subtraction.
        /// </returns>
        public static FarValue operator -(FarValue value1, FarValue value2)
        {
            var result = value1;
            result._segment -= value2._segment;
            result._offset -= value2._offset;
            result.Normalize();
            return result;
        }

        /// <summary>
        /// Subtracts the specified <see cref="float"/> from the specified <see cref="FarValue"/>.
        /// </summary>
        /// <param name="value1">The value to subtract from.</param>
        /// <param name="value2">The value to subtract.</param>
        /// <returns>
        /// The result of the subtraction.
        /// </returns>
        /// <remarks>
        /// This is implemented explicitly because a more efficient implementation
        /// is possible than when relying on the implicit cast of float to <see cref="FarValue"/>.
        /// </remarks>
        public static FarValue operator -(FarValue value1, float value2)
        {
            var result = value1;
            result._offset -= value2;
            result.Normalize();
            return result;
        }

        /// <summary>
        /// Subtracts the specified <see cref="int"/> from the specified <see cref="FarValue"/>.
        /// </summary>
        /// <param name="value1">The value to subtract from.</param>
        /// <param name="value2">The value to subtract.</param>
        /// <returns>
        /// The result of the subtraction.
        /// </returns>
        /// <remarks>
        /// This is implemented explicitly because a more efficient implementation
        /// is possible than when relying on the implicit cast of int to <see cref="FarValue"/>.
        /// </remarks>
        public static FarValue operator -(FarValue value1, int value2)
        {
            var result = value1;
            result._segment -= value2 / SegmentSize;
            result._offset -= value2 % SegmentSize;
            result.Normalize();
            return result;
        }

        /// <summary>
        /// Multiplies the specified <see cref="FarValue"/> with the specified <see cref="float"/>.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>The result of the multiplication.</returns>
        public static FarValue operator *(FarValue value1, float value2)
        {
            FarValue result;
            var segment = value1._segment * value2;
            result._segment = (int)segment;
            result._offset = value1._offset * value2 + (segment - result._segment) * SegmentSize;
            result.Normalize();
            return result;
        }

        /// <summary>
        /// Multiplies the specified <see cref="FarValue"/> with the specified <see cref="int"/>.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>The result of the multiplication.</returns>
        public static FarValue operator *(FarValue value1, int value2)
        {
            FarValue result;
            result._segment = value1._segment * value2;
            result._offset = value1._offset * value2;
            result.Normalize();
            return result;
        }

        /// <summary>
        /// Divides the specified <see cref="FarValue"/> by the specified <see cref="float"/>.
        /// </summary>
        /// <param name="value1">The dividend.</param>
        /// <param name="value2">The divisor.</param>
        /// <returns>
        /// The result of the division.
        /// </returns>
        public static FarValue operator /(FarValue value1, float value2)
        {
            FarValue result;
            var segment = value1._segment / value2;
            result._segment = (int)segment;
            result._offset = value1._offset / value2 + (segment - result._segment) * SegmentSize;
            result.Normalize();
            return result;
        }

        /// <summary>
        /// Performs the modulo operation on the specified <see cref="FarValue"/> as the
        /// dividend and the specified <see cref="int"/> as the divisor.
        /// </summary>
        /// <param name="value1">The dividend.</param>
        /// <param name="value2">The divisor.</param>
        /// <returns>The remainder.</returns>
        public static FarValue operator %(FarValue value1, int value2)
        {
            // Make sure all our values are positive, but remember the sign of the input value.
            var sign = value1 < 0 ? -1 : 1;
            value1 = value1 < 0 ? -value1 : value1;
            value2 = value2 < 0 ? -value2 : value2;

            FarValue result;
            result._segment = 0;
            result._offset = ((value1._offset % value2) + (((value1._segment % value2) * (SegmentSize % value2)) % value2)) % value2;
            result.Normalize();
            return result * sign;
        }

        /// <summary>
        /// Checks whether the first specified <see cref="FarValue"/> is smaller than the second specified <see cref="FarValue"/>.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>
        ///   <c>true</c> if the first value is smaller than the second one; otherwise <c>false</c>.
        /// </returns>
        public static bool operator <(FarValue value1, FarValue value2)
        {
            if (value1._segment < value2._segment)
            {
                return true;
            }
            if (value2._segment < value1._segment)
            {
                return false;
            }
            return value1._offset < value2._offset;
        }

        /// <summary>
        /// Checks whether the first specified <see cref="FarValue"/> is larger than the second specified <see cref="FarValue"/>.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>
        ///   <c>true</c> if the first value is larger than the second one; otherwise <c>false</c>.
        /// </returns>
        public static bool operator >(FarValue value1, FarValue value2)
        {
            if (value1._segment > value2._segment)
            {
                return true;
            }
            if (value2._segment > value1._segment)
            {
                return false;
            }
            return value1._offset > value2._offset;
        }

        /// <summary>
        /// Checks whether the first specified <see cref="FarValue"/> is smaller or equal than the second specified <see cref="FarValue"/>.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>
        ///   <c>true</c> if the first value is smaller or equal than the second one; otherwise <c>false</c>.
        /// </returns>
        public static bool operator <=(FarValue value1, FarValue value2)
        {
            return value1 == value2 || value1 < value2;
        }

        /// <summary>
        /// Checks whether the first specified <see cref="FarValue"/> is larger or equal than the second specified <see cref="FarValue"/>.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>
        ///   <c>true</c> if the first value is larger or equal than the second one; otherwise <c>false</c>.
        /// </returns>
        public static bool operator >=(FarValue value1, FarValue value2)
        {
            return value1 == value2 || value1 > value2;
        }

        /// <summary>
        /// Returns whether the two specified <see cref="FarValue"/>s are equal.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>
        /// Whether the two values are the same.
        /// </returns>
        /// <remarks>
        /// This should only be used for identity testing. Use typical
        /// floating point epsilon checks otherwise, e.g.
        /// <c>(float)(a - b) &lt; Epsilon</c>.
        /// </remarks>
        public static bool operator ==(FarValue value1, FarValue value2)
        {
            return value1._segment == value2._segment &&
                   value1._offset == value2._offset;
        }

        /// <summary>
        /// Returns whether the two specified <see cref="FarValue"/>s are unequal.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>
        /// Whether the two values are different.
        /// </returns>
        /// <remarks>
        /// This should only be used for identity testing. Use typical
        /// floating point epsilon checks otherwise, e.g.
        /// <c>(float)(a - b) &lt; Epsilon</c>.
        /// </remarks>
        public static bool operator !=(FarValue value1, FarValue value2)
        {
            return value1._segment != value2._segment ||
                   value1._offset != value2._offset;
        }

        #endregion

        #region Type Conversions (Casts)

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.Single"/> to <see cref="Engine.FarMath.FarValue"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        /// <remarks>
        /// Although convenient, one might want to change this to be explicit
        /// at some point, to check if all set values are actually intentional
        /// conversions.
        /// </remarks>
        public static implicit operator FarValue(float value)
        {
            FarValue result;
            result._segment = 0;
            result._offset = value;
            result.Normalize();
            return result;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.Int32"/> to <see cref="Engine.FarMath.FarValue"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        /// <remarks>
        /// Although convenient, one might want to change this to be explicit
        /// at some point, to check if all set values are actually intentional
        /// conversions.
        /// </remarks>
        public static implicit operator FarValue(int value)
        {
            FarValue result;
            result._segment = value / SegmentSize;
            result._offset = value % SegmentSize;
            result.Normalize();
            return result;
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="System.Double"/> to <see cref="Engine.FarMath.FarValue"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        /// <remarks>
        /// This first casts to float, leading to a possible loss in precision.
        /// </remarks>
        public static explicit operator FarValue(double value)
        {
            return (float)value;
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="System.Int64"/> to <see cref="Engine.FarMath.FarValue"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        /// <remarks>
        /// This first casts to int, leading to a possible loss in precision.
        /// </remarks>
        public static explicit operator FarValue(long value)
        {
            return (int)value;
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="System.UInt32"/> to <see cref="Engine.FarMath.FarValue"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        /// <remarks>
        /// This first casts to int, leading to a possible loss in precision.
        /// </remarks>
        public static explicit operator FarValue(uint value)
        {
            return (int)value;
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="System.UInt64"/> to <see cref="Engine.FarMath.FarValue"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        /// <remarks>
        /// This first casts to int, leading to a possible loss in precision.
        /// </remarks>
        public static explicit operator FarValue(ulong value)
        {
            return (int)value;
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="Engine.FarMath.FarValue"/> to <see cref="System.Int32"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>
        /// The result of the conversion, which has the same properties as
        /// with a cast from float to int (decimals are dropped).
        /// </returns>
        public static explicit operator int(FarValue value)
        {
            return (int)(value._segment * SegmentSize + value._offset);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="Engine.FarMath.FarValue"/> to <see cref="System.Single"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        /// <remarks>
        /// Only use when absolutely necessary, leads to loss of precision.
        /// </remarks>
        public static explicit operator float(FarValue value)
        {
            return value._segment * SegmentSize + value._offset;
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="Engine.FarMath.FarValue"/> to <see cref="System.Double"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        /// <remarks>
        /// Only use when absolutely necessary, leads to loss of precision.
        /// </remarks>
        public static explicit operator double(FarValue value)
        {
            // Cast offset to double for best possible precision.
            return value._segment * SegmentSize + (double)value._offset;
        }

        #endregion

        #region Serialization / Hashing

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>The packet after writing.</returns>
        public Packet Packetize(Packet packet)
        {
            packet.Write(_segment);
            packet.Write(_offset);

            return packet;
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public void Depacketize(Packet packet)
        {
            _segment = packet.ReadInt32();
            _offset = packet.ReadSingle();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public void Hash(Hasher hasher)
        {
            hasher.Put(_segment);
            hasher.Put(_offset);
        }

        #endregion

        #region Compare / Equality

        /// <summary>
        /// Compares the current object with another object of the same type.
        /// </summary>
        /// <returns>
        /// A value that indicates the relative order of the objects being compared. The return value has the following meanings: Value Meaning Less than zero This object is less than the <paramref name="other"/> parameter.Zero This object is equal to <paramref name="other"/>. Greater than zero This object is greater than <paramref name="other"/>. 
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public int CompareTo(FarValue other)
        {
            return (int)(this - other);
        }

        /// <summary>
        /// Compares two <see cref="FarValue"/>s for equality.
        /// </summary>
        /// <param name="other">The other <see cref="FarValue"/>.</param>
        /// <returns>
        ///   <c>true</c> if the <see cref="FarValue"/>s are equal; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(FarValue other)
        {
            return _segment == other._segment &&
                   _offset == other._offset;
        }

        /// <summary>
        /// Compares two <see cref="FarValue"/>s for equality.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified value is a <see cref="FarValue"/> and the
        /// <see cref="FarValue"/>s are equal; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return (obj is FarValue) && Equals((FarValue)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return _segment.GetHashCode() + _offset.GetHashCode();
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return ((float)this).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <param name="detailed">if set to <c>true</c> returnes a detailed representation.</param>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public string ToString(bool detailed)
        {
            return detailed ? string.Format("{{Segment:{0} Offet:{1}}}", _segment, _offset.ToString(System.Globalization.CultureInfo.InvariantCulture)) : ToString();
        }

        #endregion
    }
}
