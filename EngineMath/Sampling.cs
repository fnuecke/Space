using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Math
{
    /// <summary>
    /// Base class for samplings, performing general tasks.
    /// </summary>
    /// <typeparam name="T">The actual type of the sampling.</typeparam>
    public abstract class AbstractSampling<T> : ISampling<T>, IEnumerable<T>
        where T : struct
    {
        #region Properties

        /// <summary>Gets the last sample put into this sampling.</summary>
        public T Last
        {
            get { return SampleCount == 0 ? default(T) : Samples[(_writeIndex - 1 + SampleCount) % SampleCount]; }
        }

        /// <summary>Gets the sampling point with the highest value in this sampling.</summary>
        public T Max
        {
            get { return SampleCount == 0 ? default(T) : Samples.Take(SampleCount).Max(); }
        }

        #endregion

        #region Fields

        /// <summary>
        /// The list of samples.
        /// </summary>
        protected readonly T[] Samples;

        /// <summary>
        /// The highest index to which we have samplings. This is used
        /// to avoid bias towards the initial values (0).
        /// </summary>
        protected int SampleCount;

        /// <summary>
        /// Next index to write new values.
        /// </summary>
        private int _writeIndex;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new sampling object for the given number of samples.
        /// </summary>
        /// <param name="samples">number of samples to keep track of.</param>
        protected AbstractSampling(int samples)
        {
            Samples = new T[samples];
        }

        /// <summary>Resets this sampling to its initial, empty state.</summary>
        public void Reset()
        {
            _writeIndex = 0;
            SampleCount = 0;
        }

        #endregion

        #region Sampling

        /// <summary>
        /// Push a new sample to the list, pushing out the oldest one.
        /// </summary>
        /// <param name="sample">the sample to push.</param>
        public void Put(T sample)
        {
            Samples[_writeIndex++] = sample;
            SampleCount = (_writeIndex > SampleCount) ? _writeIndex : SampleCount;
            if (_writeIndex == Samples.Length)
            {
                _writeIndex = 0;
            }
        }

        #endregion

        #region Measuring

        /// <summary>
        /// Measure the average (arithmetic mean) based on the samples.
        /// </summary>
        /// <returns>the average over the last samples.</returns>
        public abstract double Mean();

        /// <summary>
        /// Measure the average (arithmetic mean) based on the samples which
        /// lie in the specified interval.
        /// </summary>
        /// <returns>the average over the last samples.</returns>
        public abstract double Mean(T min, T max);

        /// <summary>
        /// Measure the median based on the samples.
        /// </summary>
        /// <returns>the average over the last samples.</returns>
        public T Median()
        {
            if (SampleCount == 0)
            {
                return default(T);
            }
            var sorted = new T[SampleCount];
            for (var i = 0; i < SampleCount; i++)
            {
                sorted[i] = Samples[i];
            }
            Array.Sort(sorted);
            var mid = sorted.Length / 2;
            return (sorted.Length & 1) == 0 ? MedianEvenImpl(sorted[mid - 1], sorted[mid]) : sorted[mid];
        }

        public double StandardDeviation()
        {
            if (SampleCount < 2)
            {
                return 0;
            }
            var mean = Mean();
            double acc = 0;
            for (var i = 0; i < SampleCount; i++)
            {
                acc = StandardDeviationSumImpl(acc, Samples[i], mean);
            }
            return acc / (SampleCount - 1.0);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// To be implemented in subclasses, builds the average of the two
        /// values, for the median with an even length number of samples.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected abstract T MedianEvenImpl(T a, T b);

        protected abstract double StandardDeviationSumImpl(double acc, T value, double mean);

        #endregion

        #region Implementation of IEnumerable

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<T> GetEnumerator()
        {
            return new SamplingEnumerator(this);
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Enumerator implementation that supports resetting.
        /// </summary>
        private sealed class SamplingEnumerator : IEnumerator<T>
        {
            #region Properties

            /// <summary>
            /// Gets the element in the collection at the current position of the enumerator.
            /// </summary>
            /// <returns>
            /// The element in the collection at the current position of the enumerator.
            /// </returns>
            public T Current { get; private set; }

            /// <summary>
            /// Gets the current element in the collection.
            /// </summary>
            /// <returns>
            /// The current element in the collection.
            /// </returns>
            /// <exception cref="T:System.InvalidOperationException">The enumerator is positioned before the first element of the collection or after the last element.</exception><filterpriority>2</filterpriority>
            object IEnumerator.Current
            {
                get { return Current; }
            }

            #endregion

            #region Fields

            /// <summary>
            /// The sampling we enumerate.
            /// </summary>
            private AbstractSampling<T> _sampling;

            /// <summary>
            /// The current position of the enumerator (i.e. the bucket with the current value).
            /// </summary>
            private int _position;

            /// <summary>
            /// How many buckets we already iterated. The position wraps, so we have
            /// to keep track of this manually (well, there are other ways but this is
            /// clearer).
            /// </summary>
            private int _counter;

            #endregion

            #region Constructor

            /// <summary>
            /// Initializes a new instance of the <see cref="AbstractSampling&lt;T&gt;.SamplingEnumerator"/> class.
            /// </summary>
            /// <param name="sampling">The sampling.</param>
            public SamplingEnumerator(AbstractSampling<T> sampling)
            {
                _sampling = sampling;
                Reset();
            }

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            /// <filterpriority>2</filterpriority>
            public void Dispose()
            {
                _sampling = null;
            }

            #endregion

            #region Methods

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns>
            /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
            /// </returns>
            /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception><filterpriority>2</filterpriority>
            public bool MoveNext()
            {
                _position = (_position + 1) % _sampling.Samples.Length;
                ++_counter;
                if (_counter < _sampling.SampleCount)
                {
                    Current = _sampling.Samples[_position];
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception><filterpriority>2</filterpriority>
            public void Reset()
            {
                _position = (_sampling._writeIndex - _sampling.SampleCount + _sampling.Samples.Length) % _sampling.Samples.Length;
                _counter = 0;
            }

            #endregion
        }

        #endregion
    }

    /// <summary>
    /// Utility class to measure some properties of a sample set of integers.
    /// </summary>
    public sealed class IntSampling : AbstractSampling<int>
    {
        #region Constructor

        /// <summary>
        /// Creates a new sampling object for the given number of samples.
        /// </summary>
        /// <param name="samples">number of samples to keep track of.</param>
        public IntSampling(int samples)
            : base(samples)
        {
        }

        #endregion

        #region Measuring

        /// <summary>
        /// Measure the average (arithmetic mean) based on the samples.
        /// </summary>
        /// <returns>the average over the last samples.</returns>
        public override double Mean()
        {
            if (SampleCount == 0)
            {
                return 0;
            }
            return Mean(int.MinValue, int.MaxValue);
        }

        /// <summary>
        /// Measure the average (arithmetic mean) based on the samples which
        /// lie in the specified interval.
        /// </summary>
        /// <returns>the average over the last samples.</returns>
        public override double Mean(int min, int max)
        {
            int sum = 0;
            int count = 0;
            for (int i = 0; i < SampleCount; ++i)
            {
                if (Samples[i] >= min && Samples[i] <= max)
                {
                    sum += Samples[i];
                    ++count;
                }
            }
            if (count > 0)
            {
                return sum / (double)count;
            }
            else
            {
                return (min + max) / (double)2;
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Computes average of two integers.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The first value.</param>
        /// <returns>The average of the two.</returns>
        protected override int MedianEvenImpl(int a, int b)
        {
            return (a + b) / 2;
        }

        /// <summary>
        /// Computes part of the sum for the standard deviation.
        /// </summary>
        /// <param name="acc">The accumulator.</param>
        /// <param name="value">The value to add.</param>
        /// <param name="mean">The mean.</param>
        /// <returns>The accumulator with the added value.</returns>
        protected override double StandardDeviationSumImpl(double acc, int value, double mean)
        {
            double diff = (value - mean);
            return acc + diff * diff;
        }

        #endregion
    }

    /// <summary>
    /// Utility class to measure some properties of a sample set of doubles.
    /// </summary>
    public sealed class FloatSampling : AbstractSampling<float>
    {
        #region Constructor

        /// <summary>
        /// Creates a new sampling object for the given number of samples.
        /// </summary>
        /// <param name="samples">number of samples to keep track of.</param>
        public FloatSampling(int samples)
            : base(samples)
        {
        }

        #endregion

        #region Measuring

        /// <summary>
        /// Measure the average (arithmetic mean) based on the samples.
        /// </summary>
        /// <returns>the average over the last samples.</returns>
        public override double Mean()
        {
            if (SampleCount == 0)
            {
                return 0;
            }
            return Mean(float.NegativeInfinity, float.PositiveInfinity);
        }

        /// <summary>
        /// Measure the average (arithmetic mean) based on the samples which
        /// lie in the specified interval.
        /// </summary>
        /// <returns>the average over the last samples.</returns>
        public override double Mean(float min, float max)
        {
            float sum = 0;
            var count = 0;
            for (var i = 0; i < SampleCount; ++i)
            {
                if (Samples[i] >= min && Samples[i] <= max)
                {
                    sum += Samples[i];
                    ++count;
                }
            }
            if (count > 0)
            {
                return sum / count;
            }
            else
            {
                return (min + max) / 2;
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Computes average of two doubles.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The first value.</param>
        /// <returns>The average of the two.</returns>
        protected override float MedianEvenImpl(float a, float b)
        {
            return (a + b) / 2;
        }

        /// <summary>
        /// Computes part of the sum for the standard deviation.
        /// </summary>
        /// <param name="acc">The accumulator.</param>
        /// <param name="value">The value to add.</param>
        /// <param name="mean">The mean.</param>
        /// <returns>The accumulator with the added value.</returns>
        protected override double StandardDeviationSumImpl(double acc, float value, double mean)
        {
            var diff = (value - mean);
            return acc + diff * diff;
        }

        #endregion
    }

    /// <summary>
    /// Utility class to measure some properties of a sample set of doubles.
    /// </summary>
    public sealed class DoubleSampling : AbstractSampling<double>
    {
        #region Constructor

        /// <summary>
        /// Creates a new sampling object for the given number of samples.
        /// </summary>
        /// <param name="samples">number of samples to keep track of.</param>
        public DoubleSampling(int samples)
            : base(samples)
        {
        }

        #endregion

        #region Measuring

        /// <summary>
        /// Measure the average (arithmetic mean) based on the samples.
        /// </summary>
        /// <returns>the average over the last samples.</returns>
        public override double Mean()
        {
            if (SampleCount == 0)
            {
                return 0;
            }
            return Mean(double.NegativeInfinity, double.PositiveInfinity);
        }

        /// <summary>
        /// Measure the average (arithmetic mean) based on the samples which
        /// lie in the specified interval.
        /// </summary>
        /// <returns>the average over the last samples.</returns>
        public override double Mean(double min, double max)
        {
            double sum = 0;
            var count = 0;
            for (var i = 0; i < SampleCount; ++i)
            {
                if (Samples[i] >= min && Samples[i] <= max)
                {
                    sum += Samples[i];
                    ++count;
                }
            }
            if (count > 0)
            {
                return sum / count;
            }
            else
            {
                return (min + max) / 2;
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Computes average of two doubles.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The first value.</param>
        /// <returns>The average of the two.</returns>
        protected override double MedianEvenImpl(double a, double b)
        {
            return (a + b) / 2;
        }

        /// <summary>
        /// Computes part of the sum for the standard deviation.
        /// </summary>
        /// <param name="acc">The accumulator.</param>
        /// <param name="value">The value to add.</param>
        /// <param name="mean">The mean.</param>
        /// <returns>The accumulator with the added value.</returns>
        protected override double StandardDeviationSumImpl(double acc, double value, double mean)
        {
            var diff = (value - mean);
            return acc + diff * diff;
        }

        #endregion
    }
}
