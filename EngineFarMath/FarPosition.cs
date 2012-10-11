using System;
using Microsoft.Xna.Framework;

namespace Engine.FarMath
{
    /// <summary>
    /// Represents a "far position", a two dimensional coordinate based on <see cref="FarValue"/>s.
    /// 
    /// <para>
    /// Use this type to store absolute positions in a very large world. It is not
    /// recommended to use this as an alternative to normal <see cref="Vector2"/>s for
    /// performance reasons. Whenever values are relative to each other, and therefore
    /// in a range of values that can safely be expressed with normal floats, one
    /// should convert to <see cref="Vector2"/> and continue with that.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Actual "far math" is implemented via the <see cref="FarValue"/> struct.
    /// </remarks>
    public struct FarPosition
    {
        #region Constants

        /// <summary>
        /// Represents the origin, equivalent to a zero vector.
        /// </summary>
        public static FarPosition Zero
        {
            get { return ConstZero; }
        }

        /// <summary>
        /// Keep as private field to avoid manipulation.
        /// </summary>
        private static readonly FarPosition ConstZero = new FarPosition {X = 0, Y = 0};

        #endregion

        #region Fields

        /// <summary>
        /// The x-coordinate of this <see cref="FarPosition"/>.
        /// </summary>
        public FarValue X;

        /// <summary>
        /// The y-coordinate of this <see cref="FarPosition"/>.
        /// </summary>
        public FarValue Y;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="FarPosition"/> struct.
        /// </summary>
        /// <param name="x">The x-coordinate of the position.</param>
        /// <param name="y">The y-coordinate of the position.</param>
        public FarPosition(FarValue x, FarValue y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FarPosition"/> struct.
        /// </summary>
        /// <param name="xy">The x- and y-coordinate of the position.</param>
        public FarPosition(FarValue xy)
        {
            X = xy;
            Y = xy;
        }

        #endregion

        #region Transformation

        /// <summary>
        /// Transforms the specified position by the specified matrix and translation.
        /// </summary>
        /// <param name="position">The position to transform.</param>
        /// <param name="transform">The transformation to apply.</param>
        /// <returns>
        /// The result of the transformation.
        /// </returns>
        public static FarPosition Transform(FarPosition position, FarTransform transform)
        {
            return Transform(position + transform.Translation, transform.Matrix);
        }

        /// <summary>
        /// Transforms the specified position by the specified matrix and translation.
        /// </summary>
        /// <param name="position">The position to transform.</param>
        /// <param name="transform">The transformation to apply.</param>
        /// <param name="result">The result of the transformation.</param>
        public static void Transform(ref FarPosition position, ref FarTransform transform, out FarPosition result)
        {
            var translatedPosition = position + transform.Translation;
            Transform(ref translatedPosition, ref transform.Matrix, out result);
        }

        /// <summary>
        /// Transforms the specified position by the specified matrix.
        /// </summary>
        /// <param name="position">The position to transform.</param>
        /// <param name="transform">The transformation to apply.</param>
        /// <returns>
        /// The result of the transformation.
        /// </returns>
        public static FarPosition Transform(FarPosition position, Matrix transform)
        {
            FarPosition result;
            result.X = position.X * transform.M11 + position.Y * transform.M21 + transform.M41;
            result.Y = position.X * transform.M12 + position.Y * transform.M22 + transform.M42;
            return result;
        }

        /// <summary>
        /// Transforms the specified position by the specified matrix.
        /// </summary>
        /// <param name="position">The position to transform.</param>
        /// <param name="transform">The transformation to apply.</param>
        /// <param name="result">The result.</param>
        /// <returns>
        /// The result of the transformation.
        /// </returns>
        public static void Transform(ref FarPosition position, ref Matrix transform, out FarPosition result)
        {
            result.X = position.X * transform.M11 + position.Y * transform.M21 + transform.M41;
            result.Y = position.X * transform.M12 + position.Y * transform.M22 + transform.M42;
        }

        /// <summary>
        /// Transforms the specified position by the specified matrix.
        /// </summary>
        /// <param name="sourceArray">The source array.</param>
        /// <param name="transform">The transformation to apply.</param>
        /// <param name="destinationArray">The destination array.</param>
        public static void Transform(FarPosition[] sourceArray, ref Matrix transform, FarPosition[] destinationArray)
        {
            if (sourceArray == null)
            {
                throw new ArgumentNullException("sourceArray");
            }
            if (destinationArray == null)
            {
                throw new ArgumentNullException("destinationArray");
            }
            if (destinationArray.Length < sourceArray.Length)
            {
                throw new ArgumentException("Target array too small.");
            }
            for (var i = 0; i < sourceArray.Length; i++)
            {
                destinationArray[i].X = sourceArray[i].X * transform.M11 + sourceArray[i].Y * transform.M21 + transform.M41;
                destinationArray[i].Y = sourceArray[i].X * transform.M12 + sourceArray[i].Y * transform.M22 + transform.M42;
            }

        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns the component-wise minimum of the two specified values.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>
        /// The component-wise minimum.
        /// </returns>
        public static FarPosition Min(FarPosition value1, FarPosition value2)
        {
            FarPosition result;
            result.X = (value1.X < value2.X) ? value1.X : value2.X;
            result.Y = (value1.Y < value2.Y) ? value1.Y : value2.Y;
            return result;
        }

        /// <summary>
        /// Returns the component-wise minimum of the two specified values.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <param name="result">The component-wise minimum.</param>
        public static void Min(ref FarPosition value1, ref FarPosition value2, out FarPosition result)
        {
            result.X = (value1.X < value2.X) ? value1.X : value2.X;
            result.Y = (value1.Y < value2.Y) ? value1.Y : value2.Y;
        }

        /// <summary>
        /// Returns the component-wise maximum of the two specified values.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>
        /// The component-wise maximum.
        /// </returns>
        public static FarPosition Max(FarPosition value1, FarPosition value2)
        {
            FarPosition result;
            result.X = (value1.X > value2.X) ? value1.X : value2.X;
            result.Y = (value1.Y > value2.Y) ? value1.Y : value2.Y;
            return result;
        }

        /// <summary>
        /// Returns the component-wise maximum of the two specified values.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <param name="result">The component-wise maximum.</param>
        public static void Max(ref FarPosition value1, ref FarPosition value2, out FarPosition result)
        {
            result.X = (value1.X > value2.X) ? value1.X : value2.X;
            result.Y = (value1.Y > value2.Y) ? value1.Y : value2.Y;
        }

        /// <summary>
        /// Computes the dot product of the specified <see cref="FarPosition"/> and <see cref="Vector2"/>.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>The dot product.</returns>
        public static FarValue Dot(Vector2 value1, FarPosition value2)
        {
            return value1.X * value2.X + value1.Y * value2.Y;

            // We could save two FarValue multiplications (one FarPosition multiplication),
            // but that would come at the cost of precision for positions primarily far
            // along a single axis:
            //  ax + by
            //= ax + bx + by - bx
            //= x(a + b) + b(y - x)
            //= (a + b)(x + b/(a + b)(y - x))
            //return (value1.X + value1.Y) * (value2.X + value1.Y / (value1.X + value1.Y) * (float)(value2.Y - value2.X));
        }

        /// <summary>
        /// Computes the dot product of the specified <see cref="FarPosition"/> and <see cref="Vector2"/>.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>The dot product.</returns>
        public static FarValue Dot(FarPosition value1, Vector2 value2)
        {
            return Dot(value2, value1);
        }

        /// <summary>
        /// Clamps the specified value between the specified minimum and maximum.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The minimum of the interval to clamp to.</param>
        /// <param name="max">The maximum of the interval to clamp to.</param>
        /// <returns>The clamped value.</returns>
        public static FarPosition Clamp(FarPosition value, FarPosition min, FarPosition max)
        {
            FarPosition result;
            FarValue.Clamp(ref value.X, ref min.X, ref max.X, out result.X);
            FarValue.Clamp(ref value.Y, ref min.Y, ref max.Y, out result.Y);
            return result;
        }

        /// <summary>
        /// Clamps the specified value between the specified minimum and maximum.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The minimum of the interval to clamp to.</param>
        /// <param name="max">The maximum of the interval to clamp to.</param>
        /// <param name="result">The clamped value.</param>
        public static void Clamp(ref FarPosition value, ref FarPosition min, ref FarPosition max, out FarPosition result)
        {
            FarValue.Clamp(ref value.X, ref min.X, ref max.X, out result.X);
            FarValue.Clamp(ref value.Y, ref min.Y, ref max.Y, out result.Y);
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
        public static FarPosition Lerp(FarPosition value1, FarPosition value2, float amount)
        {
            FarPosition result;
            FarValue.Lerp(ref value1.X, ref value2.X, amount, out result.X);
            FarValue.Lerp(ref value1.Y, ref value2.Y, amount, out result.Y);
            return result;
        }

        /// <summary>
        /// Performs a linear interpolation between the specified values.
        /// </summary>
        /// <param name="value1">The value to interpolate from.</param>
        /// <param name="value2">The value to interpolate towards.</param>
        /// <param name="amount">The amount to interpolate.</param>
        /// <param name="result">The interpolated value.</param>
        public static void Lerp(ref FarPosition value1, ref FarPosition value2, float amount, out FarPosition result)
        {
            FarValue.Lerp(ref value1.X, ref value2.X, amount, out result.X);
            FarValue.Lerp(ref value1.Y, ref value2.Y, amount, out result.Y);
        }

        /// <summary>
        /// Performs a <see href="https://en.wikipedia.org/wiki/Smoothstep"/>
        /// interpolation between the specified values.
        /// </summary>
        /// <param name="value1">The value to interpolate from.</param>
        /// <param name="value2">The value to interpolate towards.</param>
        /// <param name="amount">The amount to interpolate.</param>
        /// <returns>The interpolated value.</returns>
        public static FarPosition SmoothStep(FarPosition value1, FarPosition value2, float amount)
        {
            FarPosition result;
            FarValue.SmoothStep(ref value1.X, ref value2.X, amount, out result.X);
            FarValue.SmoothStep(ref value1.Y, ref value2.Y, amount, out result.Y);
            return result;
        }

        /// <summary>
        /// Performs a <see href="https://en.wikipedia.org/wiki/Smoothstep"/>
        /// interpolation between the specified values.
        /// </summary>
        /// <param name="value1">The value to interpolate from.</param>
        /// <param name="value2">The value to interpolate towards.</param>
        /// <param name="amount">The amount to interpolate.</param>
        /// <param name="result">The interpolated value.</param>
        public static void SmoothStep(ref FarPosition value1, ref FarPosition value2, float amount, out FarPosition result)
        {
            FarValue.SmoothStep(ref value1.X, ref value2.X, amount, out result.X);
            FarValue.SmoothStep(ref value1.Y, ref value2.Y, amount, out result.Y);
        }

        #endregion

        #region Operators

        /// <summary>
        /// Returns the inverse of the specified value (i.e. a vector in
        /// the opposite direction).
        /// </summary>
        /// <param name="value">The value to invert.</param>
        /// <returns>
        /// The inverse value.
        /// </returns>
        public static FarPosition operator -(FarPosition value)
        {
            FarPosition result;
            result.X = -value.X;
            result.Y = -value.Y;
            return result;
        }

        /// <summary>
        /// Adds the two specified <see cref="FarPosition"/>s together.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>
        /// The sum of the two values.
        /// </returns>
        public static FarPosition operator +(FarPosition value1, FarPosition value2)
        {
            var result = value1;
            result.X += value2.X;
            result.Y += value2.Y;
            return result;
        }

        /// <summary>
        /// Adds the specified <see cref="FarPosition"/> and a <see cref="Vector2"/> together.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>
        /// The sum of the two values.
        /// </returns>
        public static FarPosition operator +(FarPosition value1, Vector2 value2)
        {
            var result = value1;
            result.X += value2.X;
            result.Y += value2.Y;
            return result;
        }

        /// <summary>
        /// Adds the specified <see cref="FarPosition"/> and a <see cref="Vector2"/> together.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>
        /// The sum of the two values.
        /// </returns>
        public static FarPosition operator +(Vector2 value1, FarPosition value2)
        {
            var result = value2;
            result.X += value1.X;
            result.Y += value1.Y;
            return result;
        }

        /// <summary>
        /// Adds the specified <see cref="FarPosition"/> and the specified <see cref="Point"/> together.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>
        /// The sum of the two values.
        /// </returns>
        public static FarPosition operator +(FarPosition value1, Point value2)
        {
            var result = value1;
            result.X += value2.X;
            result.Y += value2.Y;
            return result;
        }

        /// <summary>
        /// Adds the specified <see cref="FarPosition"/> and a <see cref="Point"/> together.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>
        /// The sum of the two values.
        /// </returns>
        public static FarPosition operator +(Point value1, FarPosition value2)
        {
            var result = value2;
            result.X += value1.X;
            result.Y += value1.Y;
            return result;
        }

        /// <summary>
        /// Subtracts the first specified <see cref="FarPosition"/> from the second specified <see cref="FarPosition"/>.
        /// </summary>
        /// <param name="value1">The value to subtract from.</param>
        /// <param name="value2">The value to subtract.</param>
        /// <returns>
        /// The result of the subtraction.
        /// </returns>
        public static FarPosition operator -(FarPosition value1, FarPosition value2)
        {
            var result = value1;
            result.X -= value2.X;
            result.Y -= value2.Y;
            return result;
        }

        /// <summary>
        /// Subtracts the specified <see cref="Vector2"/> from the specified <see cref="FarPosition"/>.
        /// </summary>
        /// <param name="value1">The value to subtract from.</param>
        /// <param name="value2">The value to subtract.</param>
        /// <returns>
        /// The result of the subtraction.
        /// </returns>
        public static FarPosition operator -(FarPosition value1, Vector2 value2)
        {
            var result = value1;
            result.X -= value2.X;
            result.Y -= value2.Y;
            return result;
        }

        /// <summary>
        /// Subtracts the specified <see cref="Vector2"/> from the specified <see cref="FarPosition"/>.
        /// </summary>
        /// <param name="value1">The value to subtract from.</param>
        /// <param name="value2">The value to subtract.</param>
        /// <returns>
        /// The result of the subtraction.
        /// </returns>
        public static FarPosition operator -(Vector2 value1, FarPosition value2)
        {
            FarPosition result;
            result.X = value1.X - value2.X;
            result.Y = value1.Y - value2.Y;
            return result;
        }

        /// <summary>
        /// Subtracts the specified <see cref="Point"/> from the specified <see cref="FarPosition"/>.
        /// </summary>
        /// <param name="value1">The value to subtract from.</param>
        /// <param name="value2">The value to subtract.</param>
        /// <returns>
        /// The result of the subtraction.
        /// </returns>
        public static FarPosition operator -(FarPosition value1, Point value2)
        {
            var result = value1;
            result.X -= value2.X;
            result.Y -= value2.Y;
            return result;
        }

        /// <summary>
        /// Subtracts the specified <see cref="Point"/> from the specified <see cref="FarPosition"/>.
        /// </summary>
        /// <param name="value1">The value to subtract from.</param>
        /// <param name="value2">The value to subtract.</param>
        /// <returns>
        /// The result of the subtraction.
        /// </returns>
        public static FarPosition operator -(Point value1, FarPosition value2)
        {
            FarPosition result;
            result.X = value1.X - value2.X;
            result.Y = value1.Y - value2.Y;
            return result;
        }

        /// <summary>
        /// Multiplies the specified <see cref="FarPosition"/> with the specified <see cref="float"/>.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>
        /// The result of the multiplication.
        /// </returns>
        public static FarPosition operator *(FarPosition value1, float value2)
        {
            var result = value1;
            result.X *= value2;
            result.Y *= value2;
            return result;
        }

        /// <summary>
        /// Multiplies the specified <see cref="FarPosition"/> with the specified <see cref="float"/>.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>
        /// The result of the multiplication.
        /// </returns>
        public static FarPosition operator *(float value1, FarPosition value2)
        {
            return value2 * value1;
        }

        /// <summary>
        /// Multiplies the specified <see cref="FarPosition"/> with the specified <see cref="Vector2"/>, component-wise.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>
        /// The result of the multiplication.
        /// </returns>
        public static FarPosition operator *(FarPosition value1, Vector2 value2)
        {
            var result = value1;
            result.X *= value2.X;
            result.Y *= value2.Y;
            return result;
        }

        /// <summary>
        /// Multiplies the specified <see cref="FarPosition"/> with the specified <see cref="Vector2"/>, component-wise.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>
        /// The result of the multiplication.
        /// </returns>
        public static FarPosition operator *(Vector2 value1, FarPosition value2)
        {
            return value2 * value1;
        }

        /// <summary>
        /// Divides the specified <see cref="FarPosition"/> by the specified <see cref="float"/>.
        /// </summary>
        /// <param name="value1">The dividend.</param>
        /// <param name="value2">The divisor.</param>
        /// <returns>
        /// The result of the division.
        /// </returns>
        public static FarPosition operator /(FarPosition value1, float value2)
        {
            var result = value1;
            result.X /= value2;
            result.Y /= value2;
            return result;
        }

        /// <summary>
        /// Returns whether the two specified <see cref="FarPosition"/>s are equal.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>
        /// Whether the two values are the same.
        /// </returns>
        /// <remarks>
        /// This should only be used for identity testing. Use typical
        /// floating point epsilon checks otherwise.
        /// </remarks>
        public static bool operator ==(FarPosition value1, FarPosition value2)
        {
            return value1.X == value2.X && value1.Y == value2.Y;
        }

        /// <summary>
        /// Returns whether the two specified <see cref="FarPosition"/>s are unequal.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>
        /// Whether the two values are different.
        /// </returns>
        /// <remarks>
        /// This should only be used for identity testing. Use typical
        /// floating point epsilon checks otherwise.
        /// </remarks>
        public static bool operator !=(FarPosition value1, FarPosition value2)
        {
            return value1.X != value2.X || value1.Y != value2.Y;
        }

        #endregion

        #region Type Conversion (Casts)

        /// <summary>
        /// Performs an implicit conversion from <see cref="Microsoft.Xna.Framework.Vector2"/> to <see cref="Engine.FarMath.FarPosition"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static explicit operator FarPosition(Vector2 value)
        {
            FarPosition result;
            result.X = value.X;
            result.Y = value.Y;
            return result;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Microsoft.Xna.Framework.Point"/> to <see cref="Engine.FarMath.FarPosition"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static explicit operator FarPosition(Point value)
        {
            FarPosition result;
            result.X = value.X;
            result.Y = value.Y;
            return result;
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="Engine.FarMath.FarPosition"/> to <see cref="Microsoft.Xna.Framework.Vector2"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        /// <remarks>
        /// Only use when absolutely necessary, leads to loss of precision.
        /// </remarks>
        public static explicit operator Vector2(FarPosition value)
        {
            Vector2 result;
            result.X = (float)value.X;
            result.Y = (float)value.Y;
            return result;
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="Engine.FarMath.FarPosition"/> to <see cref="Microsoft.Xna.Framework.Point"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        /// <remarks>
        /// Only use when absolutely necessary, leads to loss of precision.
        /// </remarks>
        public static explicit operator Point(FarPosition value)
        {
            Point result;
            result.X = (int)value.X;
            result.Y = (int)value.Y;
            return result;
        }

        #endregion

        #region Equality Overrides

        /// <summary>
        /// Compares two <see cref="FarPosition"/>s for equality.
        /// </summary>
        /// <param name="other">The other <see cref="FarPosition"/>.</param>
        /// <returns>
        ///   <c>true</c> if the <see cref="FarPosition"/>s are equal; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(FarPosition other)
        {
            return X == other.X && Y == other.Y;
        }

        /// <summary>
        /// Compares two <see cref="FarPosition"/>s for equality.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified value is a <see cref="FarPosition"/> and the
        /// <see cref="FarPosition"/>s are equal; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return (obj is FarPosition) && Equals((FarPosition)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return X.GetHashCode() + Y.GetHashCode();
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
            return string.Format("{{X:{0} Y:{1}}}", X, Y);
        }

        #endregion
    }
}
