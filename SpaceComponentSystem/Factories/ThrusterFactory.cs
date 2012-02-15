using Engine.ComponentSystem.Entities;
using Engine.Util;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Factories
{
    /// <summary>
    /// Constraints for generating thrusters.
    /// </summary>
    public sealed class ThrusterFactory : ItemFactory
    {
        #region Sampling

        /// <summary>
        /// Samples a new thruster based on these constraints.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled thruster.</returns>
        public override Entity Sample(IUniformRandom random)
        {
            var entity = new Entity();

            entity.AddComponent(new Thruster(Name, Icon, Quality));

            return SampleAttributes(entity, random);
        }

        #endregion
    }
}
