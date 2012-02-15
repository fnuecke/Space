using Engine.ComponentSystem;
using Engine.Util;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Factories
{
    /// <summary>
    /// Constraints for generating armor.
    /// </summary>
    public sealed class ArmorFactory : ItemFactory
    {
        #region Sampling

        /// <summary>
        /// Samples a new armor based on these constraints.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled armor.</returns>
        public override Entity Sample(IUniformRandom random)
        {
            var entity = base.Sample(random);

            entity.AddComponent(new Armor(Name, Icon, Quality));

            return SampleAttributes(entity, random);
        }

        #endregion
    }
}
