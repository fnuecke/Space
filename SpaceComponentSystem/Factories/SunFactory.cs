using Engine.ComponentSystem;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.FarMath;
using Engine.Math;
using Engine.Random;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Systems;
using Space.Data;

namespace Space.ComponentSystem.Factories
{
    /// <summary>
    /// Generates suns based on set constraints.
    /// </summary>
    public sealed class SunFactory : IFactory
    {
        #region General

        /// <summary>
        /// The unique name of the object type.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Radius for generated suns.
        /// </summary>
        public FloatInterval Radius;

        /// <summary>
        /// Offset from cell center for generated suns.
        /// </summary>
        public FloatInterval OffsetRadius;

        /// <summary>
        /// Mass of generated suns.
        /// </summary>
        public FloatInterval Mass;

        #endregion

        #region Sampling

        /// <summary>
        /// Samples the attributes to apply to the item.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="cellCenter">The center of the cell the sun will be inserted in.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <return>The entity with the attributes applied.</return>
        public int SampleSun(IManager manager, FarPosition cellCenter, IUniformRandom random)
        {
            var entity = manager.AddEntity();

            // Sample all values in advance, to allow reshuffling component creation
            // order in case we need to, without influencing the 'random' results.
            var radius = SampleRadius(random);
            var offset = SampleOffset(random);
            var mass = SampleMass(random);

            Vector2 surfaceRotation;
            surfaceRotation.X = (float)(random.NextDouble() - 0.5) * 2;
            surfaceRotation.Y = (float)(random.NextDouble() - 0.5) * 2;
            surfaceRotation.Normalize();

            Vector2 primaryTurbulenceRotation;
            primaryTurbulenceRotation.X = (float)(random.NextDouble() - 0.5) * 2;
            primaryTurbulenceRotation.Y = (float)(random.NextDouble() - 0.5) * 2;
            primaryTurbulenceRotation.Normalize();

            Vector2 secondaryTurbulenceRotation;
            secondaryTurbulenceRotation.X = (float)(random.NextDouble() - 0.5) * 2;
            secondaryTurbulenceRotation.Y = (float)(random.NextDouble() - 0.5) * 2;
            secondaryTurbulenceRotation.Normalize();

            manager.AddComponent<Transform>(entity).Initialize(offset + cellCenter);

            // Make it attract stuff if it has mass.
            if (mass > 0)
            {
                manager.AddComponent<Gravitation>(entity).Initialize(Gravitation.GravitationTypes.Attractor, mass);
            }

            // Make it collidable.
            manager.AddComponent<CollidableSphere>(entity).Initialize(radius, Factions.Nature.ToCollisionGroup());

            // Instantly kill stuff that touches a sun.
            manager.AddComponent<CollisionDamage>(entity).Initialize(1, float.MaxValue);

            // Make it detectable.
            manager.AddComponent<Detectable>(entity).Initialize("Textures/Radar/Icons/radar_sun");

            // Make it glow.
            manager.AddComponent<SunRenderer>(entity).Initialize(radius * 0.95f, surfaceRotation, primaryTurbulenceRotation, secondaryTurbulenceRotation);

            // Make it go whoooosh.
            manager.AddComponent<Sound>(entity).Initialize("Sun");

            // Add to indexes for lookup.
            manager.AddComponent<Index>(entity).Initialize(
                CollisionSystem.IndexGroupMask | // Can be bumped into.
                DetectableSystem.IndexGroupMask | // Can be detected.
                SoundSystem.IndexGroupMask | // Can make noise.
                CellSystem.CellDeathAutoRemoveIndexGroupMask | // Will be removed when out of bounds.
                TextureRenderSystem.IndexGroupMask,
                (int)(radius + radius));

            return entity;
        }

        /// <summary>
        /// Samples the radius of this sun.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled radius.</returns>
        private float SampleRadius(IUniformRandom random)
        {
            return (random == null) ? Radius.Low
                : MathHelper.Lerp(Radius.Low, Radius.High, (float)random.NextDouble());
        }

        /// <summary>
        /// Samples the offset of this sun.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled offset.</returns>
        private Vector2 SampleOffset(IUniformRandom random)
        {
            if (random == null)
            {
                return Vector2.Zero;
            }
            else
            {
                Vector2 offset;
                offset.X = (float)(random.NextDouble() - 0.5);
                offset.Y = (float)(random.NextDouble() - 0.5);
                offset.Normalize();
                offset *= MathHelper.Lerp(OffsetRadius.Low, OffsetRadius.High, (float)random.NextDouble());
                return offset;
            }
        }

        /// <summary>
        /// Samples the mass of this sun.
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
