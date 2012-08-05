using System;
using System.Collections;
using System.Collections.Generic;

namespace Engine.Math
{
    /// <summary>
    /// Base class for samplings, performing general tasks.
    /// </summary>
    /// <typeparam name="T">The actual type of the sampling.</typeparam>
    public abstract class AbstractSampling<T> : ISampling<T>, IEnumerable<T>
        where T : struct
    {
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
            for (int i = (_writeIndex - SampleCount + Samples.Length) % Samples.Length, j = 0; j < SampleCount; i = (i + 1) % Samples.Length, ++j)
            {
                yield return Samples[i];
            }
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
