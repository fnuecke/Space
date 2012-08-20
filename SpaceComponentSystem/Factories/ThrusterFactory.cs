using Engine.ComponentSystem;
using Engine.Random;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Factories
{
    /// <summary>
    /// Constraints for generating thrusters.
    /// </summary>
    public sealed class ThrusterFactory : ItemFactory
    {
        #region Fields

        /// <summary>
        /// Asset name of the particle effect to trigger when this thruster is
        /// active (accelerating).
        /// </summary>
        [ContentSerializer(Optional = true)]
        public string Effect;

        /// <summary>
        /// Offset for the thruster effect relative to the texture.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public Vector2 EffectOffset = Vector2.Zero;

        #endregion

        #region Sampling

        /// <summary>
        /// Samples a new thruster based on these constraints.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>
        /// The sampled thruster.
        /// </returns>
        public override int Sample(IManager manager, IUniformRandom random)
        {
            var entity = base.Sample(manager, random);

            manager.AddComponent<Thruster>(entity).Initialize(Name, Icon, Quality, SlotSize, Effect, EffectOffset);

            return SampleAttributes(manager, entity, random);
        }

        #endregion
    }
}
