using System;

namespace Engine.Random
{
    /// <summary>Interface for classes producing pseudo random numbers that follow a uniform distribution.</summary>
    public interface IUniformRandom
    {
        /// <summary>Returns a random number between 0.0 and 1.0, i.e. [0.0, 1.0).</summary>
        /// <returns>A double-precision floating point number greater than or equal to 0.0, and less than 1.0.</returns>
        double NextDouble();

        /// <summary>Returns a random number within a specified range.</summary>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">
        ///     The exclusive upper bound of the random number returned. <em>maxValue</em> must be greater than or equal to
        ///     <em>minValue</em>.
        /// </param>
        /// <returns>
        ///     A double-precision floating point number greater than or equal to <em>minValue</em>, and less than
        ///     <em>maxValue</em>; that is, the range of return values includes <em>minValue</em> but not
        ///     <em>maxValue</em>. If <em>minValue</em> equals <em>maxValue</em>,
        ///     <em>minValue</em> is returned.
        /// </returns>
        double NextDouble(double minValue, double maxValue);

        /// <summary>Returns a nonnegative random number.</summary>
        /// <returns>
        ///     A 32-bit signed integer greater than or equal to zero and less than <see cref="int.MaxValue"/>.
        /// </returns>
        int NextInt32();

        /// <summary>Returns a nonnegative random number less than the specified maximum.</summary>
        /// <param name="maxValue">
        ///     The exclusive upper bound of the random number to be generated. maxValue must be greater than or
        ///     equal to zero.
        /// </param>
        /// <returns>
        ///     A 32-bit signed integer greater than or equal to zero, and less than maxValue; that is, the range of return
        ///     values ordinarily includes zero but not maxValue. However, if maxValue equals zero, maxValue is returned.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <em>maxValue</em> is less than zero.
        /// </exception>
        int NextInt32(int maxValue);

        /// <summary>Returns a random number within a specified range.</summary>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">
        ///     The exclusive upper bound of the random number returned. <em>maxValue</em> must be greater than or equal to
        ///     <em>minValue</em>.
        /// </param>
        /// <returns>
        ///     A 32-bit signed integer greater than or equal to
        ///     <em>minValue</em> and less than <em>maxValue</em>; that is, the range of return values includes <em>minValue</em>
        ///     but not
        ///     <em>maxValue</em>. If <em>minValue</em> equals <em>maxValue</em>,
        ///     <em>minValue</em> is returned.
        /// </returns>
        int NextInt32(int minValue, int maxValue);

        /// <summary>Returns a nonnegative random number.</summary>
        /// <returns>
        ///     A 32-bit unsigned integer greater than or equal to zero and less than <see cref="uint.MaxValue"/>.
        /// </returns>
        uint NextUInt32();

        /// <summary>Returns a nonnegative random number less than the specified maximum.</summary>
        /// <param name="maxValue">
        ///     The exclusive upper bound of the random number to be generated. maxValue must be greater than or
        ///     equal to zero.
        /// </param>
        /// <returns>
        ///     A 32-bit signed integer greater than or equal to zero, and less than maxValue; that is, the range of return
        ///     values ordinarily includes zero but not maxValue. However, if maxValue equals zero, maxValue is returned.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <em>maxValue</em> is less than zero.
        /// </exception>
        uint NextUInt32(uint maxValue);

        /// <summary>Returns a random number within a specified range.</summary>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">
        ///     The exclusive upper bound of the random number returned. <em>maxValue</em> must be greater than or equal to
        ///     <em>minValue</em>.
        /// </param>
        /// <returns>
        ///     A 32-bit signed integer greater than or equal to
        ///     <em>minValue</em> and less than <em>maxValue</em>; that is, the range of return values includes <em>minValue</em>
        ///     but not
        ///     <em>maxValue</em>. If <em>minValue</em> equals <em>maxValue</em>,
        ///     <em>minValue</em> is returned.
        /// </returns>
        uint NextUInt32(uint minValue, uint maxValue);
    }
}