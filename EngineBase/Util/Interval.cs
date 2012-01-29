using System;

namespace Engine.Util
{
    /// <summary>
    /// Represents an interval of the specified type.
    /// </summary>
    /// <typeparam name="T">The interval type.</typeparam>
    public class Interval<T> where T : IComparable<T>, IEquatable<T>
    {
        /// <summary>
        /// Default 'zero' value for an interval.
        /// </summary>
        public static readonly Interval<T> Zero = new Interval<T>(default(T), default(T));

        /// <summary>
        /// The low endpoint of the interval.
        /// </summary>
        public T Low { get; private set; }

        /// <summary>
        /// The high endpoint of the interval.
        /// </summary>
        public T High { get; private set; }

        #region Constructor
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Interval&lt;T&gt;"/> struct.
        /// </summary>
        /// <param name="low">The low endpoint.</param>
        /// <param name="high">The high endpoint.</param>
        public Interval(T low, T high)
        {
            SetTo(low, high);
        }

        /// <summary>
        /// For serialization.
        /// </summary>
        public Interval()
        {
        }

        #endregion

        /// <summary>
        /// Sets the interval endpoints to the specified values.
        /// </summary>
        /// <param name="low">The low endpoint.</param>
        /// <param name="high">The high endpoint.</param>
        /// <exception cref="ArgumentException">If low is larger than high.</exception>
        public void SetTo(T low, T high)
        {
            if (low.CompareTo(high) > 0)
            {
                throw new ArgumentException("Invalid interval, the lower endpoint must be less or equal to the higher endpoint.", "low");
            }
            this.Low = low;
            this.High = high;
        }
    }
}
