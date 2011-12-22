using System;
namespace Engine.Util
{
    /// <summary>
    /// Base class for samplings, performing general tasks.
    /// </summary>
    /// <typeparam name="T">The actual type of the sampling.</typeparam>
    public abstract class AbstractSampling<T> : ISampling<T>
        where T : struct
    {
        #region Fields

        /// <summary>
        /// The list of samples.
        /// </summary>
        protected T[] samples;

        /// <summary>
        /// The highest index to which we have samplings. This is used
        /// to avoid bias towards the initial values (0).
        /// </summary>
        protected int sampleCount = 0;

        /// <summary>
        /// Next index to write new values.
        /// </summary>
        private int _writeIndex = 0;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new sampling object for the given number of samples.
        /// </summary>
        /// <param name="samples">number of samples to keep track of.</param>
        protected AbstractSampling(int samples)
        {
            this.samples = new T[samples];
        }

        #endregion

        #region Sampling

        /// <summary>
        /// Push a new sample to the list, pushing out the oldest one.
        /// </summary>
        /// <param name="sample">the sample to push.</param>
        public void Put(T sample)
        {
            samples[_writeIndex++] = sample;
            sampleCount = (_writeIndex > sampleCount) ? _writeIndex : sampleCount;
            if (_writeIndex == samples.Length)
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
            if (sampleCount == 0)
            {
                return default(T);
            }
            T[] sorted = new T[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                sorted[i] = samples[i];
            }
            Array.Sort(sorted);
            int mid = sorted.Length / 2;
            if ((sorted.Length & 1) == 0)
            {
                // Even.
                return MedianEvenImpl(sorted[mid - 1], sorted[mid]);
            }
            else
            {
                // Odd.
                return sorted[mid];
            }
        }

        public double StandardDeviation()
        {
            if (sampleCount < 2)
            {
                return 0;
            }
            double mean = Mean();
            double acc = 0;
            for (int i = 0; i < sampleCount; i++)
            {
                acc = StandardDeviationSumImpl(acc, samples[i], mean);
            }
            return acc / (double)(sampleCount - 1);
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
            if (sampleCount == 0)
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
            for (int i = 0; i < sampleCount; ++i)
            {
                if (samples[i] >= min && samples[i] <= max)
                {
                    sum += samples[i];
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

        protected override int MedianEvenImpl(int a, int b)
        {
            return (a + b) / 2;
        }

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
            if (sampleCount == 0)
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
            int count = 0;
            for (int i = 0; i < sampleCount; ++i)
            {
                if (samples[i] >= min && samples[i] <= max)
                {
                    sum += samples[i];
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

        protected override double MedianEvenImpl(double a, double b)
        {
            return (a + b) / 2;
        }

        protected override double StandardDeviationSumImpl(double acc, double value, double mean)
        {
            double diff = (value - mean);
            return acc + diff * diff;
        }

        #endregion
    }
}
