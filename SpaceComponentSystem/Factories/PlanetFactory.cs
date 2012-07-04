using System;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Components;
using Engine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Systems;

namespace Space.ComponentSystem.Factories
{
    /// <summary>
    /// Generates planetes based on set constraints.
    /// </summary>
    public sealed class PlanetFactory : IFactory
    {
        #region General

        /// <summary>
        /// The unique name of the object type.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Texture to use for generated planets.
        /// </summary>
        public string Texture;

        /// <summary>
        /// The color tint for generated planets' surface.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public Color SurfaceTint = Color.White;

        /// <summary>
        /// The atmospheric color tint for generated planets.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public Color AtmosphereTint = Color.Transparent;

        /// <summary>
        /// The radius of generated planets.
        /// </summary>
        public Interval<float> Radius;

        /// <summary>
        /// The mass of generated planets.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public Interval<float> Mass = Interval<float>.Zero;

        /// <summary>
        /// The rotation speed of generated planets.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public Interval<float> RotationSpeed = Interval<float>.Zero;

        /// <summary>
        /// The major radius of generated planets' orbits. This is scaled
        /// based on the actual oribiting distance to the sun, which is
        /// determined in the sun system factory.
        /// </summary>
        public Interval<float> MajorRadius;

        /// <summary>
        /// The minor radius of generated planets' orbits. This is scaled
        /// based on the actual oribiting distance to the sun, which is
        /// determined in the sun system factory.
        /// </summary>
        public Interval<float> MinorRadius;

        /// <summary>
        /// The offset from the base orbiting angle of the sun system.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public Interval<float> AngleOffset = Interval<float>.Zero;

        /// <summary>
        /// The travel speed of generated planets' orbits. This will be used
        /// to determine the period, based on the orbiting radius.
        /// </summary>
        public Interval<float> TravelSpeed;

        #endregion

        #region Sampling

        /// <summary>
        /// Samples the attributes to apply to the item.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="center">The entity to revolve around.</param>
        /// <param name="angle">The base angle for orbit ellipses.</param>
        /// <param name="radius">The base orbiting radius this planet will have.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <return>The entity with the attributes applied.</return>
        public int SamplePlanet(IManager manager, int center, float angle, float radius, IUniformRandom random)
        {
            var entity = manager.AddEntity();

            // Sample all values in advance, to allow reshuffling component creation
            // order in case we need to, without influencing the 'random' results.
            var planetRadius = SampleRadius(random);
            var rotationSpeed = SampleRotationSpeed(random);
            var mass = SampleMass(random);
            var majorRadius = SampleMajorRadius(random);
            var minorRadius = SampleMinorRadius(random);
            var angleOffset = SampleAngleOffset(random);
            var travelSpeed = SampleTravelSpeed(random);
            var periodOffet = (float)random.NextDouble();

            // Scale radii.
            var radiusScale = (radius + radius) / (majorRadius + minorRadius);
            majorRadius *= radiusScale;
            minorRadius *= radiusScale;

            // Get period. Figure out circumference using Ramanujan's approximation.
            var a = majorRadius;
            var b = minorRadius;
            var circumference = MathHelper.Pi * (3 * (a + b) - (float)Math.Sqrt((3 * a + b) * (a + 3 * b)));
            var period = circumference / travelSpeed * 60;

            manager.AddComponent<Transform>(entity).Initialize(manager.GetComponent<Transform>(center).Translation);
            manager.AddComponent<Spin>(entity).Initialize(MathHelper.ToRadians(rotationSpeed) / 60);
            manager.AddComponent<EllipsePath>(entity).Initialize(center, majorRadius, minorRadius, angle + angleOffset, period, MathHelper.TwoPi * periodOffet);
            manager.AddComponent<Index>(entity).
                Initialize(Detectable.IndexGroup |
                           Sound.IndexGroup |
                           CellSystem.CellDeathAutoRemoveIndex,
                           (int)(planetRadius + planetRadius));
            if (mass > 0)
            {
                manager.AddComponent<Gravitation>(entity).Initialize(Gravitation.GravitationTypes.Attractor, mass);
            }

            manager.AddComponent<Detectable>(entity).Initialize("Textures/Radar/Icons/radar_planet");
            manager.AddComponent<PlanetRenderer>(entity).Initialize(Texture, SurfaceTint, planetRadius, AtmosphereTint);

            manager.AddComponent<Sound>(entity).Initialize("Planet");

            return entity;
        }

        /// <summary>
        /// Samples the radius of this planet.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled radius.</returns>
        private float SampleRadius(IUniformRandom random)
        {
            return (random == null) ? Radius.Low
                : MathHelper.Lerp(Radius.Low, Radius.High, (float)random.NextDouble());
        }

        /// <summary>
        /// Samples the rotation speed of this planet.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled rotation speed.</returns>
        private float SampleRotationSpeed(IUniformRandom random)
        {
            return (random == null) ? RotationSpeed.Low
                : MathHelper.Lerp(RotationSpeed.Low, RotationSpeed.High, (float)random.NextDouble());
        }

        /// <summary>
        /// Samples the major radius of this planet's orbit.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled major radius.</returns>
        private float SampleMajorRadius(IUniformRandom random)
        {
            return (random == null) ? MajorRadius.Low
                : MathHelper.Lerp(MajorRadius.Low, MajorRadius.High, (float)random.NextDouble());
        }

        /// <summary>
        /// Samples the minor radius of this planet's orbit.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled minor radius.</returns>
        private float SampleMinorRadius(IUniformRandom random)
        {
            return (random == null) ? MinorRadius.Low
                : MathHelper.Lerp(MinorRadius.Low, MinorRadius.High, (float)random.NextDouble());
        }

        /// <summary>
        /// Samples the travel speed of this planet.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled travel speed.</returns>
        private float SampleTravelSpeed(IUniformRandom random)
        {
            return (random == null) ? TravelSpeed.Low
                : MathHelper.Lerp(TravelSpeed.Low, TravelSpeed.High, (float)random.NextDouble());
        }

        /// <summary>
        /// Samples the angle offset of this planet.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled angle offset.</returns>
        private float SampleAngleOffset(IUniformRandom random)
        {
            return (random == null) ? AngleOffset.Low
                : MathHelper.Lerp(AngleOffset.Low, AngleOffset.High, (float)random.NextDouble());
        }

        /// <summary>
        /// Samples the mass of this planet.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled mass.</returns>
        private float SampleMass(IUniformRandom random)
        {
            return (random == null) ? Mass.Low
                : MathHelper.Lerp(Mass.Low, Mass.High, (float)random.NextDouble());
        }

        #endregion
    }
}
