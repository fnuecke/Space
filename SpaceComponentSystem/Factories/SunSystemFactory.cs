using Engine.ComponentSystem;
using Engine.FarMath;
using Engine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Space.ComponentSystem.Factories.SunSystemFactoryTypes;

namespace Space.ComponentSystem.Factories
{
    /// <summary>
    /// Generates sun systems based on specific constraints.
    /// </summary>
    public sealed class SunSystemFactory : IFactory
    {
        #region General

        /// <summary>
        /// The unique name of the object type.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The type of sun to create in the center of this sun system/
        /// </summary>
        public string Sun;

        /// <summary>
        /// The planets to create.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public Orbit Planets;

        #endregion

        #region Sampling

        /// <summary>
        /// Samples the attributes to apply to the item.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="cellCenter">The center of the cell for which the sun system is created.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <return>The entity with the attributes applied.</return>
        public void SampleSunSystem(IManager manager, FarPosition cellCenter, IUniformRandom random)
        {
            var sun = FactoryLibrary.SampleSun(manager, Sun, cellCenter, random);

            if (Planets != null)
            {
                Planets.SampleOrbiters(manager, sun, random);
            }
        }

        #endregion
    }

    #region Orbitsystem

    namespace SunSystemFactoryTypes
    {

        /// <summary>
        /// Describes an orbit system with a dominant axis.
        /// </summary>
        public sealed class Orbit
        {
            /// <summary>
            /// The angle of generated planets' orbits.
            /// </summary>
            [ContentSerializer(Optional = true)] public Interval<float> Angle = new Interval<float>(0, 360);

            /// <summary>
            /// Objects in this orbit system.
            /// </summary>
            [ContentSerializer(FlattenContent = true, CollectionItemName = "Orbiter")] public Orbiter[] Orbiters;

            internal void SampleOrbiters(IManager manager, int center, IUniformRandom random)
            {
                // Get the dominant angle.
                var dominantAngle = SampleAngle(random);

                // Create orbiters.
                foreach (var orbiter in Orbiters)
                {
                    if (orbiter.ChanceToExist > random.NextDouble())
                    {
                        var entity = FactoryLibrary.SamplePlanet(manager, orbiter.PlanetName, center, dominantAngle,
                                                                 orbiter.SampleOrbitRadius(random), random);

                        // Recurse.
                        if (orbiter.Moons != null)
                        {
                            orbiter.Moons.SampleOrbiters(manager, entity, random);
                        }
                    }
                }
            }

            /// <summary>
            /// Samples the angle of this planet's orbit.
            /// </summary>
            /// <param name="random">The randomizer to use.</param>
            /// <returns>The sampled angle.</returns>
            private float SampleAngle(IUniformRandom random)
            {
                return MathHelper.ToRadians((random == null)
                                                ? Angle.Low
                                                : MathHelper.Lerp(Angle.Low, Angle.High, (float)random.NextDouble()));
            }
        }

        /// <summary>
        /// Describes a single orbiting object.
        /// </summary>
        public sealed class Orbiter
        {
            /// <summary>
            /// The name of the planet type.
            /// </summary>
            public string PlanetName;

            /// <summary>
            /// The radius at which this object orbits.
            /// </summary>
            public Interval<float> OrbitRadius;

            /// <summary>
            /// The probability with which an actual instance is created in a system.
            /// </summary>
            [ContentSerializer(Optional = true)] public float ChanceToExist = 1;

            /// <summary>
            /// Child orbits, revolving around this object.
            /// </summary>
            [ContentSerializer(Optional = true, SharedResource = true)] public Orbit Moons;

            /// <summary>
            /// Samples the angle of this planet's orbit.
            /// </summary>
            /// <param name="random">The randomizer to use.</param>
            /// <returns>The sampled angle.</returns>
            internal float SampleOrbitRadius(IUniformRandom random)
            {
                return (random == null)
                           ? OrbitRadius.Low
                           : MathHelper.Lerp(OrbitRadius.Low, OrbitRadius.High, (float)random.NextDouble());
            }
        }

    }

    #endregion
}
