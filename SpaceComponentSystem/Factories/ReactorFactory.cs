using Engine.ComponentSystem;
using Engine.Util;
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
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled reactor.</returns>
        public override Entity Sample(IUniformRandom random)
        {
            var entity = new Entity();

            entity.AddComponent(new Reactor(Name, Icon, Quality));

            return SampleAttributes(entity, random);
        }

        #endregion
    }
}
