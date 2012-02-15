using Engine.ComponentSystem.Entities;
using Engine.Util;

namespace Space.ComponentSystem.Factories
{
    /// <summary>
    /// Constraints for generating shields.
    /// </summary>
    public sealed class ShieldFactory : ItemFactory
    {
        #region Sampling

        /// <summary>
        /// Samples a new shield based on these constraints.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled shield.</returns>
        public override Entity Sample(IUniformRandom random)
        {
            throw new System.NotImplementedException();
        }

        #endregion
    }
}
