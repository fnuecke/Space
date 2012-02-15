using Engine.ComponentSystem;
using Engine.Util;
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
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled sensor.</returns>
        public override Entity Sample(IUniformRandom random)
        {
            var entity = new Entity();

            entity.AddComponent(new Sensor(Name, Icon, Quality));

            return SampleAttributes(entity, random);
        }

        #endregion
    }
}
