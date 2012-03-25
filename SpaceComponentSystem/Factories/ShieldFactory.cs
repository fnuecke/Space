using Engine.ComponentSystem;
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
        /// <param name="manager"></param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>
        /// The sampled shield.
        /// </returns>
        public override int Sample(IManager manager, IUniformRandom random)
        {
            throw new System.NotImplementedException();
        }

        #endregion
    }
}
