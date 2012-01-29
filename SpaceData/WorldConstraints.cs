using Engine.Util;
namespace Space.Data
{
    /// <summary>
    /// Static parameters for cell population (system generation).
    /// </summary>
    public class WorldConstraints
    {
        #region Fields
        
        /// <summary>
        /// The chance we spawn multiple suns in a system.
        /// </summary>
        public float SolarSystemMultiSunChance;

        /// <summary>
        /// The number of suns a system may have at max.
        /// </summary>
        public int SolarSystemMaxSuns;

        /// <summary>
        /// The average size of a sun's radius.
        /// </summary>
        public float SunRadiusMean;

        /// <summary>
        /// The allowed deviation from the mean when generating a sun.
        /// </summary>
        public float SunRadiusStdDev;

        /// <summary>
        /// Mass to attribute to a sun for it's size (base on the radius,
        /// but computing the volume from that).
        /// </summary>
        public float SunMassFactor;

        /// <summary>
        /// Mass to attribute to another astronomical object for it's size
        /// (base on the radius, but computing the volume from that).
        /// </summary>
        public float PlanetMassFactor;

        /// <summary>
        /// The average number of planets per solar system.
        /// </summary>
        public int PlanetsMean;

        /// <summary>
        /// The standard deviation of the number of planets per solar system.
        /// </summary>
        public int PlanetsStdDev;

        /// <summary>
        /// The average size of a planet's radius.
        /// </summary>
        public float PlanetRadiusMean;

        /// <summary>
        /// The allowed deviation from the mean when generating a planet.
        /// </summary>
        public float PlanetRadiusStdDev;

        /// <summary>
        /// The average mean radius of a planet's orbit.
        /// </summary>
        public float PlanetOrbitMean;
        
        /// <summary>
        /// The allowed deviation from the planet's orbit.
        /// </summary>
        public float PlanetOrbitStdDev;

        /// <summary>
        /// The average eccentricity of a planet's orbit (relation of minor
        /// radius to major radius).
        /// </summary>
        public float PlanetOrbitEccentricityMean;

        /// <summary>
        /// The allowed deviation from the planet's orbit's eccentricity.
        /// </summary>
        public float PlanetOrbitEccentricityStdDev;

        /// <summary>
        /// The average deviation of a planet's orbit angle from the dominant
        /// orbiting angle in its solar system.
        /// </summary>
        public float PlanetOrbitAngleDeviationMean;

        /// <summary>
        /// The allowed deviation from the deviation of a planet's orbit angle
        /// from the dominant orbiting angle in its solar system.
        /// </summary>
        public float PlanetOrbitAngleDeviationStdDev;

        /// <summary>
        /// The average distance of a Station to a Planet
        /// </summary>
        public float StationOrbitMean;

        /// <summary>
        /// The averageDeviation of the Stations orbit
        /// </summary>
        public float StationOrbitStdDev;

        /// <summary>
        /// The Chance that a Planet has a Station
        /// </summary>
        public float StationChance;

        /// <summary>
        /// The average number of moons per planet.
        /// </summary>
        public int MoonsMean;

        /// <summary>
        /// The standard deviation of the number of moons per planet.
        /// </summary>
        public int MoonsStdDev;

        /// <summary>
        /// The mean size of a moon as a fraction of the planet it'll belong to.
        /// </summary>
        public float MoonRadiusMean;

        /// <summary>
        /// The standard deviation of a moon's size.
        /// </summary>
        public float MoonRadiusStdDev;

        /// <summary>
        /// The mean major radius of the orbit ellipse of a moon.
        /// </summary>
        public float MoonOrbitMean;

        /// <summary>
        /// The standard deviation of the major radius of a moon's orbit ellipse.
        /// </summary>
        public float MoonOrbitStdDev;

        /// <summary>
        /// The average eccentricity of a moon's orbit (relation of minor
        /// radius to major radius).
        /// </summary>
        public float MoonOrbitEccentricityMean;

        /// <summary>
        /// The allowed deviation from the moon's orbit's eccentricity.
        /// </summary>
        public float MoonOrbitEccentricityStdDev;

        /// <summary>
        /// The average deviation of a moon's orbit angle from the dominant
        /// orbiting angle for its planet.
        /// </summary>
        public float MoonOrbitAngleDeviationMean;

        /// <summary>
        /// The allowed deviation from the deviation of a moon's orbit angle
        /// from the dominant orbiting angle for its planet.
        /// </summary>
        public float MoonOrbitAngleDeviationStdDev;
        
        #endregion

        #region Sampling

        public float SampleSunRadius(IGaussianRandom random)
        {
            return (float)random.NextSampleClamped(SunRadiusMean, SunRadiusStdDev);
        }

        public int SamplePlanets(IGaussianRandom random)
        {
            return (int)random.NextSampleClamped(PlanetsMean, PlanetsStdDev);
        }
        
        public float SamplePlanetRadius(IGaussianRandom random)
        {
            return (float)random.NextSampleClamped(PlanetRadiusMean, PlanetRadiusStdDev);
        }
        
        public float SamplePlanetOrbit(IGaussianRandom random)
        {
            return (float)random.NextSampleClamped(PlanetOrbitMean, PlanetOrbitStdDev);
        }
        
        public float SamplePlanetOrbitEccentricity(IGaussianRandom random)
        {
            return (float)random.NextSampleClamped(PlanetOrbitEccentricityMean, PlanetOrbitEccentricityStdDev);
        }
        
        public float SamplePlanetOrbitAngleDeviation(IGaussianRandom random)
        {
            return (float)random.NextSampleClamped(PlanetOrbitAngleDeviationMean, PlanetOrbitAngleDeviationStdDev);
        }

        public bool SampleStation(IUniformRandom random)
        {
            return (random.NextDouble() < StationChance);
        }

        public float SampleStationOrbit(IGaussianRandom random)
        {
            return (float)random.NextSampleClamped(StationOrbitMean, StationOrbitStdDev);
        }

        public int SampleMoons(IGaussianRandom random)
        {
            return (int)random.NextSampleClamped(MoonsMean, MoonsStdDev);
        }

        public float SampleMoonRadius(IGaussianRandom random)
        {
            return (float)random.NextSampleClamped(MoonRadiusMean, MoonRadiusStdDev);
        }
        
        public float SampleMoonOrbit(IGaussianRandom random)
        {
            return (float)random.NextSampleClamped(MoonOrbitMean, MoonOrbitStdDev);
        }

        public float SampleMoonOrbitEccentricity(IGaussianRandom random)
        {
            return (float)random.NextSampleClamped(MoonOrbitEccentricityMean, MoonOrbitEccentricityStdDev);
        }

        public float SampleMoonOrbitAngleDeviation(IGaussianRandom random)
        {
            return (float)random.NextSampleClamped(MoonOrbitAngleDeviationMean, MoonOrbitAngleDeviationStdDev);
        }

        #endregion
    }
}
