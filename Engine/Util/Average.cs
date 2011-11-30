namespace Engine.Util
{
    /// <summary>
    /// Utility class to measure the average over a set number of integers.
    /// </summary>
    public sealed class Average
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
        /// Creates a new averaging object for the given number of samples.
        /// </summary>
        /// <param name="samples">number of samples to keep track of.</param>
        public Average(int samples)
        {
            this.samples = new int[samples];
        }

        /// <summary>
        /// Push a new sample to the list.
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
        /// Measure the average based on the last <c>n</c> pushed samples.
        /// </summary>
        /// <returns>the average over the last samples.</returns>
        public int Measure()
        {
            int sum = 0;
            foreach (var sample in samples)
            {
                sum += sample;
            }
            return sum / samples.Length;
        }
    }
}
