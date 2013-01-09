using Engine.ComponentSystem;
using Engine.Random;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Factories
{
    /// <summary>Constraints for generating thrusters.</summary>
    public sealed class ThrusterFactory : ItemFactory
    {
        #region Sampling

        /// <summary>Samples a new thruster based on these constraints.</summary>
        /// <param name="manager">The manager.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled thruster.</returns>
        public override int Sample(IManager manager, IUniformRandom random)
        {
            var entity = base.Sample(manager, random);

            manager.AddComponent<Thruster>(entity)
                   .Initialize(Name, Icon, Quality, RequiredSlotSize, ModelOffset, ModelBelowParent);

            return entity;
        }

        #endregion
    }
}