using Engine.ComponentSystem;
using Engine.Random;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Factories
{
    /// <summary>
    /// Constraints for generating sensors.
    /// </summary>
    public sealed class SensorFactory : ItemFactory
    {
        #region Sampling

        /// <summary>
        /// Samples a new sensor based on these constraints.
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>
        /// The sampled sensor.
        /// </returns>
        public override int Sample(IManager manager, IUniformRandom random)
        {
            var entity = base.Sample(manager, random);

            manager.AddComponent<Sensor>(entity).Initialize(Name, Icon, Quality);

            return SampleAttributes(manager, entity, random);
        }

        #endregion
    }
}
