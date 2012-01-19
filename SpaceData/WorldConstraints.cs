using Engine.Util;
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
        /// The average mass of a sun.
        /// </summary>
        public int SunMassMean;

        /// <summary>
        /// The standard deviation of the mass of a sun.
        /// </summary>
        public int SunMassStdDev;

        /// <summary>
        /// Mass to attribute to another astronomical object for it's size
        /// (base on the radius, but computing the volume from that).
        /// </summary>
        public float MassPerVolume;

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

        public float SampleSunMass(IGaussianRandom gaussian)
        {
            return (float)gaussian.NextSampleClamped(SunMassMean, SunMassStdDev);
        }

        public int SamplePlanets(IGaussianRandom gaussian)
        {
            return (int)gaussian.NextSampleClamped(PlanetsMean, PlanetsStdDev);
        }
        
        public float SamplePlanetRadius(IGaussianRandom gaussian)
        {
            return (float)gaussian.NextSampleClamped(PlanetRadiusMean, PlanetRadiusStdDev);
        }
        
        public float SamplePlanetOrbit(IGaussianRandom gaussian)
        {
            return (float)gaussian.NextSampleClamped(PlanetOrbitMean, PlanetOrbitStdDev);
        }
        
        public float SamplePlanetOrbitEccentricity(IGaussianRandom gaussian)
        {
            return (float)gaussian.NextSampleClamped(PlanetOrbitEccentricityMean, PlanetOrbitEccentricityStdDev);
        }
        
        public float SamplePlanetOrbitAngleDeviation(IGaussianRandom gaussian)
        {
            return (float)gaussian.NextSampleClamped(PlanetOrbitAngleDeviationMean, PlanetOrbitAngleDeviationStdDev);
        }

        public bool SampleStation(IUniformRandom twister)
        {
            return (twister.NextDouble() < StationChance);
        }

        public float SampleStationOrbit(IGaussianRandom gaussian)
        {
            return (float)gaussian.NextSampleClamped(StationOrbitMean, StationOrbitStdDev);
        }
        public int SampleMoons(IGaussianRandom gaussian)
        {
            return (int)gaussian.NextSampleClamped(MoonsMean, MoonsStdDev);
        }

        public float SampleMoonRadius(IGaussianRandom gaussian)
        {
            return (float)gaussian.NextSampleClamped(MoonRadiusMean, MoonRadiusStdDev);
        }
        
        public float SampleMoonOrbit(IGaussianRandom gaussian)
        {
            return (float)gaussian.NextSampleClamped(MoonOrbitMean, MoonOrbitStdDev);
        }

        public float SampleMoonOrbitEccentricity(IGaussianRandom gaussian)
        {
            return (float)gaussian.NextSampleClamped(MoonOrbitEccentricityMean, MoonOrbitEccentricityStdDev);
        }

        public float SampleMoonOrbitAngleDeviation(IGaussianRandom gaussian)
        {
            return (float)gaussian.NextSampleClamped(MoonOrbitAngleDeviationMean, MoonOrbitAngleDeviationStdDev);
        }
    }
}
