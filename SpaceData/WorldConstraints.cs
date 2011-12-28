namespace Space.Data
{
    /// <summary>
    /// Static parameters for cell population (system generation).
    /// </summary>
    public class WorldConstraints
    {
        public float SolarSystemMultiSunChance;
        /// <summary>
        /// The number of suns a system may have at max.
        /// </summary>
        public int SolarSystemMaxSuns;

        /// <summary>
        /// The average size of a planet's radius.
        /// </summary>
        public float PlanetRadiusMean;

        /// <summary>
        /// The allowed deviation from the mean when generating a planet,
        /// as a fraction of the average planet size.
        /// </summary>
        public float PlanetRadiusStdDev;

        /// <summary>
        /// The Average Orbit size
        /// </summary>
        public float PlanetOrbitMean;
        
        /// <summary>
        /// The upward deviation from the minimum buffer (being: next lower
        /// orbit + (that planet's moon's orbit + radius if it has one, else
        /// the planet's orbit + radius).
        /// </summary>
        public float PlanetOrbitStdDev;

        /// <summary>
        /// The mean size of a moon as a fraction of the planet it'll belong to.
        /// </summary>
        public float MoonRadiusMean;

        /// <summary>
        /// The standard deviation of a moon's size as the fraction of its
        /// radius.
        /// </summary>
        public float MoonRadiusStdDevFraction;

        /// <summary>
        /// The mean size of a moon as a fraction of the planet it'll belong to.
        /// </summary>
        public float MoonOrbitMean;

        /// <summary>
        /// The standard deviation of a moon's size as the fraction of its
        /// radius.
        /// </summary>
        public float MoonOrbitStdDevFraction;
    }
}
