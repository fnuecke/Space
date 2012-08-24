using Engine.ComponentSystem;
using Engine.Random;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Factories
{
    /// <summary>
    /// Constraints for generating reactors.
    /// </summary>
    public sealed class ReactorFactory : ItemFactory
    {
        #region Sampling

        /// <summary>
        /// Samples a new reactor based on these constraints.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>
        /// The sampled reactor.
        /// </returns>
        public override int Sample(IManager manager, IUniformRandom random)
        {
            var entity = base.Sample(manager, random);

            manager.AddComponent<Reactor>(entity).Initialize(Name, Icon, Quality, RequiredSlotSize,
                                                             ModelOffset.HasValue ? ModelOffset.Value : Vector2.Zero,
                                                             ModelBelowParent);

            return SampleAttributes(manager, entity, random);
        }

        #endregion
    }
}
