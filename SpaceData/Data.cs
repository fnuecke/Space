using Engine.Math;

namespace Space.Data
{
    public class WorldConstaints
    {

        public ulong WorldSeed;
        /// <summary>
        /// Average overall radius of a solar system.
        /// </summary>
        public Fixed SolarSystemRadiusMean;

        /// <summary>
        /// The allowed deviation from the mean for a solar system, as a
        /// fraction of it's mean size.
        /// </summary>
        public Fixed SolarSystemRadiusStdDevFraction;

        /// <summary>
        /// The chance, in percent, that a solar system has more than one sun.
        /// </summary>
        public Fixed SolarSystemMultiSunChance;

        /// <summary>
        /// The number of suns a system may have at max.
        /// </summary>
        public int SolarSystemMaxSuns;

        /// <summary>
        /// The average size of a planet's radius.
        /// </summary>
        public Fixed PlanetRadiusMean;

        /// <summary>
        /// The allowed deviation from the mean when generating a planet,
        /// as a fraction of the average planet size.
        /// </summary>
        public Fixed PlanetRadiusStdDev;
        
        /// <summary>
        /// The upward deviation from the minimum buffer (being: next lower
        /// orbit + (that planet's moon's orbit + radius if it has one, else
        /// the planet's orbit + radius).
        /// </summary>
        public Fixed PlanetOrbitStdDev;

        /// <summary>
        /// The mean size of a moon as a fraction of the planet it'll belong to.
        /// </summary>
        public Fixed MoonRadiusMean;

        /// <summary>
        /// The standard deviation of a moon's size as the fraction of its
        /// radius.
        /// </summary>
        public Fixed MoonRadiusStdDevFraction;
    }
}
