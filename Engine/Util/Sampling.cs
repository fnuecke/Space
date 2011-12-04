using System;
namespace Engine.Util
{
    /// <summary>
    /// Utility class to measure some properties of a sample set of integers.
    /// </summary>
    public sealed class IntSampling : ISampling<int>
    {
        /// <summary>
        /// The list of samples.
        /// </summary>
        private int[] samples;

        /// <summary>
        /// Next index to write new values.
        /// </summary>
        private int writeIndex = 0;

        /// <summary>
        /// Creates a new sampling object for the given number of samples.
        /// </summary>
        /// <param name="samples">number of samples to keep track of.</param>
        public IntSampling(int samples)
        {
            this.samples = new int[samples];
        }

        /// <summary>
        /// Creates a new sampling object for the given number of samples,
        /// setting all samples to an initial value.
        /// </summary>
        /// <param name="samples">number of samples to keep track of.</param>
        /// <param name="initial">initial value to set samplings to.</param>
        public IntSampling(int samples, int initial)
        {
            this.samples = new int[samples];
            for (int i = 0; i < samples; i++)
            {
                this.samples[i] = initial;
            }
        }

        /// <summary>
        /// Push a new sample to the list, pushing out the oldest one.
        /// </summary>
        /// <param name="sample">the sample to push.</param>
        public void Put(int sample)
        {
            samples[writeIndex++] = sample;
            if (writeIndex == samples.Length)
            {
                writeIndex = 0;
            }
        }

        /// <summary>
        /// Measure the average (arithmetic mean) based on the samples.
        /// </summary>
        /// <returns>the average over the last samples.</returns>
        public int Mean()
        {
            int sum = 0;
            foreach (var sample in samples)
            {
                sum += sample;
            }
            return sum / samples.Length;
        }

        /// <summary>
        /// Measure the median based on the samples.
        /// </summary>
        /// <returns>the average over the last samples.</returns>
        public int Median()
        {
            int[] sorted = (int[])samples.Clone();
            Array.Sort(sorted);
            int mid = sorted.Length / 2;
            if ((sorted.Length & 1) == 0)
            {
                // Even.
                return (sorted[mid - 1] + sorted[mid]) / 2;
            }
            else
            {
                // Odd.
                return sorted[mid];
            }
        }
    }


    /// <summary>
    /// Utility class to measure some properties of a sample set of doubles.
    /// </summary>
    public sealed class DoubleSampling : ISampling<double>
    {
        /// <summary>
        /// The list of samples.
        /// </summary>
        private double[] samples;

        /// <summary>
        /// Next index to write new values.
        /// </summary>
        private int writeIndex = 0;

        /// <summary>
        /// Creates a new sampling object for the given number of samples.
        /// </summary>
        /// <param name="samples">number of samples to keep track of.</param>
        public DoubleSampling(int samples)
        {
            this.samples = new double[samples];
        }

        /// <summary>
        /// Creates a new sampling object for the given number of samples,
        /// setting all samples to an initial value.
        /// </summary>
        /// <param name="samples">number of samples to keep track of.</param>
        /// <param name="initial">initial value to set samplings to.</param>
        public DoubleSampling(int samples, double initial)
        {
            this.samples = new double[samples];
            for (int i = 0; i < samples; i++)
            {
                this.samples[i] = initial;
            }
        }

        /// <summary>
        /// Push a new sample to the list, pushing out the oldest one.
        /// </summary>
        /// <param name="sample">the sample to push.</param>
        public void Put(double sample)
        {
            samples[writeIndex++] = sample;
            if (writeIndex == samples.Length)
            {
                writeIndex = 0;
            }
        }

        /// <summary>
        /// Measure the average (arithmetic mean) based on the samples.
        /// </summary>
        /// <returns>the average over the last samples.</returns>
        public double Mean()
        {
            double sum = 0;
            foreach (var sample in samples)
            {
                sum += sample;
            }
            return sum / samples.Length;
        }

        /// <summary>
        /// Measure the median based on the samples.
        /// </summary>
        /// <returns>the average over the last samples.</returns>
        public double Median()
        {
            double[] sorted = (double[])samples.Clone();
            Array.Sort(sorted);
            int mid = sorted.Length / 2;
            if ((sorted.Length & 1) == 0)
            {
                // Even.
                return (sorted[mid - 1] + sorted[mid]) / 2;
            }
            else
            {
                // Odd.
                return sorted[mid];
            }
        }
    }
}
