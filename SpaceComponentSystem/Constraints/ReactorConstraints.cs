using Engine.ComponentSystem.Entities;
using Engine.Util;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Constraints
{
    /// <summary>
    /// Constraints for generating reactors.
    /// </summary>
    public sealed class ReactorConstraints : ItemConstraints
    {
        #region Sampling

        /// <summary>
        /// Samples a new reactor based on these constraints.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled reactor.</returns>
        public override Entity Sample(IUniformRandom random)
        {
            var entity = new Entity();

            entity.AddComponent(new Reactor());

            return SampleAttributes(entity, random);
        }

        #endregion
    }
}
