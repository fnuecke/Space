namespace Engine.Random
{
    /// <summary>
    /// Interface for pseudo random number generators producing normally
    /// distributed values.
    /// </summary>
    public interface IGaussianRandom
    {
        /// <summary>
        /// Get the next sample value from a standard normal distribution.
        /// </summary>
        /// <returns></returns>
        double NextSample();
        
        /// <summary>
        /// Get the next sample value from the gaussian distribution.
        /// </summary>
        /// <param name="mu">The distribution's mean.</param>
        /// <param name="sigma">The distribution's standard deviation.</param>
        double NextSample(double mu, double sigma);
        
        /// <summary>
        /// Get the next sample value from the gaussian distribution, but
        /// clamp it to the 2*sigma interval around mu.
        /// </summary>
        /// <param name="mu">The distribution's mean.</param>
        /// <param name="sigma">The distribution's standard deviation.</param>
        double NextSampleClamped(double mu, double sigma);
    }
}
