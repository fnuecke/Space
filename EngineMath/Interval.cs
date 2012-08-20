using System;
using System.ComponentModel;

namespace Engine.Math
{
    /// <summary>
    /// Represents an interval of the specified type.
    /// </summary>
    /// <typeparam name="T">The interval type.</typeparam>
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public sealed class Interval<T> where T : IComparable<T>, IEquatable<T>
    {
        #region Constants
        
        /// <summary>
        /// Default 'zero' value for an interval.
        /// </summary>
        public static readonly Interval<T> Zero = new Interval<T>(default(T), default(T));

        #endregion

        #region Properties
        
        /// <summary>
        /// The low endpoint of the interval.
        /// </summary>
        [Description("The lower inclusive bound of the interval.")]
        public T Low
        {
            get { return _low; }
            set { SetTo(value, _high); }
        }

        /// <summary>
        /// The high endpoint of the interval.
        /// </summary>
        [Description("The upper inclusive bound of the interval.")]
        public T High
        {
            get { return _high; }
            set { SetTo(_low, value); }
        }

        #endregion

        #region Backing fields

        private T _low;

        private T _high;

        #endregion

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

        #region Methods
        
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
            _low = low;
            _high = high;
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
            return "[" + _low + ", " + _high + "]";
        }

        #endregion
    }
}
