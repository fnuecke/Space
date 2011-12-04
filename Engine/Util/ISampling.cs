namespace Engine.Util
{
    /// <summary>
    /// Interface for samplings, providing a read-only representation.
    /// </summary>
    /// <typeparam name="T">the underlying type of the sampling.</typeparam>
    public interface ISampling<T>
    {
        /// <summary>
        /// Measure the average (arithmetic mean) based on the samples.
        /// </summary>
        /// <returns>the average over the last samples.</returns>
        T Mean();

        /// <summary>
        /// Measure the median based on the samples.
        /// </summary>
        /// <returns>the average over the last samples.</returns>
        T Median();
    }
}
