using Engine.ComponentSystem.Entities;
using Engine.Util;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Constraints
{
    /// <summary>
    /// Constraints for generating armor.
    /// </summary>
    public sealed class ArmorConstraints : ItemConstraints
    {
        #region Sampling

        /// <summary>
        /// Samples a new armor based on these constraints.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled armor.</returns>
        public override Entity Sample(IUniformRandom random)
        {
            var entity = new Entity();

            entity.AddComponent(new Armor());

            return SampleAttributes(entity, random);
        }

        #endregion
    }
}
