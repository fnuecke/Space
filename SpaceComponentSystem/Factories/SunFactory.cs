
using Engine.ComponentSystem;
using Engine.ComponentSystem.Components;
using Engine.Util;
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
        public Interval<float> Radius;

        /// <summary>
        /// Offset from cell center for generated suns.
        /// </summary>
        public Interval<float> OffsetRadius;

        /// <summary>
        /// Mass of generated suns.
        /// </summary>
        public Interval<float> Mass;

        #endregion
        
        #region Sampling

        /// <summary>
        /// Samples the attributes to apply to the item.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="cellCenter">The center of the cell the sun will be inserted in.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <return>The entity with the attributes applied.</return>
        public int SampleSun(IManager manager, Vector2 cellCenter, IUniformRandom random)
        {
            var entity = manager.AddEntity();

            manager.AddComponent<Transform>(entity).Initialize(SampleOffset(random) + cellCenter);
            manager.AddComponent<Spin>(entity);
            manager.AddComponent<Index>(entity).Initialize(
                Detectable.IndexGroup |
                CellSystem.CellDeathAutoRemoveIndex |
                Factions.Nature.ToCollisionIndexGroup());
            manager.AddComponent<Gravitation>(entity).Initialize(Gravitation.GravitationTypes.Attractor, SampleMass(random));

            var radius = SampleRadius(random);

            manager.AddComponent<CollidableSphere>(entity).Initialize(radius, Factions.Nature.ToCollisionGroup());
            manager.AddComponent<CollisionDamage>(entity).Initialize(1, float.MaxValue);

            manager.AddComponent<Detectable>(entity).Initialize("Textures/Radar/Icons/radar_sun");

            manager.AddComponent<SunRenderer>(entity).Initialize(radius);

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
