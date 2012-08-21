using System;
using System.ComponentModel;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.Math;
using Engine.Random;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Systems;
using Space.Util;

namespace Space.ComponentSystem.Factories
{
    /// <summary>
    /// Generates planetes based on set constraints.
    /// </summary>
    [DefaultProperty("Name")]
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
        public FloatInterval Radius;

        /// <summary>
        /// The mass of generated planets.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public FloatInterval Mass = FloatInterval.Zero;

        /// <summary>
        /// The rotation speed of generated planets.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public FloatInterval RotationSpeed = FloatInterval.Zero;

        /// <summary>
        /// The eccentricity of generated planets' orbits. A value of
        /// 0 means it's a circle, 1 means it's totally flat (a line).
        /// </summary>
        public FloatInterval Eccentricity;

        /// <summary>
        /// The offset from the base orbiting angle of the sun system.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public FloatInterval AngleOffset = FloatInterval.Zero;

        /// <summary>
        /// The travel speed of generated planets' orbits. This will be used
        /// to determine the period, based on the orbiting radius.
        /// </summary>
        public FloatInterval TravelSpeed;

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
            var eccentricity = SampleEccentricity(random);
            var angleOffset = SampleAngleOffset(random);
            var travelSpeed = SampleTravelSpeed(random);
            var periodOffet = (float)random.NextDouble();

            Vector2 surfaceRotation;
            surfaceRotation.X = (float)(random.NextDouble() - 0.5);
            surfaceRotation.Y = (float)(random.NextDouble() - 0.5);
            surfaceRotation.Normalize();
            surfaceRotation *= rotationSpeed;

            // Compute major radius and focus distance for an ellipse fitting
            // inside a unit circle with the focus point as its center.
            // We know that e = f/a and because of the unit circle assumption
            // with the focus as its center it must follow that a + f = 1.
            var a = 1 / (1 + eccentricity);
            var f = 1 - a;

            // We can the compute the minor radius for that very ellipse. We
            // know that f = sqrt(a * a - b * b), which can be transformed to
            // b = sqrt(a * a - f * f).
            var b = (float)Math.Sqrt(a * a - f * f);

            // We then scale the minor and major radii to fit into our actual
            // orbit radius.
            a *= radius;
            b *= radius;

            // Get period. Figure out circumference using Ramanujan's approximation.
            var circumference = MathHelper.Pi * (3 * (a + b) - (float)Math.Sqrt((3 * a + b) * (a + 3 * b)));
            var period = circumference / travelSpeed * Settings.TicksPerSecond;

            // Give it a position and rotation.
            manager.AddComponent<Transform>(entity).Initialize(((Transform)manager.GetComponent(center, Transform.TypeId)).Translation);

            // Make it rotate.
            manager.AddComponent<Spin>(entity).Initialize(MathHelper.ToRadians(rotationSpeed) / Settings.TicksPerSecond);

            // Make it move around its parent.
            manager.AddComponent<EllipsePath>(entity).Initialize(center, a, b, angle + angleOffset, period, MathHelper.TwoPi * periodOffet);

            // Make it attract stuff if it has mass.
            if (mass > 0)
            {
                manager.AddComponent<Gravitation>(entity).Initialize(Gravitation.GravitationTypes.Attractor, mass);
            }

            // Make it detectable.
            manager.AddComponent<Detectable>(entity).Initialize("Textures/Radar/Icons/radar_planet");

            // Make it visible.
            manager.AddComponent<PlanetRenderer>(entity).Initialize(Texture, SurfaceTint, planetRadius, AtmosphereTint, surfaceRotation);

            // Let it rap.
            manager.AddComponent<Sound>(entity).Initialize("Planet");

            // Add to indexes for lookup.
            manager.AddComponent<Index>(entity).Initialize(
                DetectableSystem.IndexGroupMask | // Can be detected.
                SoundSystem.IndexGroupMask | // Can make noise.
                CellSystem.CellDeathAutoRemoveIndexGroupMask | // Will be removed when out of bounds.
                TextureRenderSystem.IndexGroupMask,
                (int)(planetRadius + planetRadius));

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
        /// Samples the eccentricity of this planet's orbit.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled major radius.</returns>
        private float SampleEccentricity(IUniformRandom random)
        {
            return (random == null) ? Eccentricity.Low
                : MathHelper.Lerp(Eccentricity.Low, Eccentricity.High, (float)random.NextDouble());
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
