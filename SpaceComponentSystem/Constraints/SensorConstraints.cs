using Engine.ComponentSystem.Entities;
using Engine.Util;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Constraints
{
    /// <summary>
    /// Constraints for generating sensors.
    /// </summary>
    public sealed class SensorConstraints : ItemConstraints
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

            entity.AddComponent(new Sensor());

            return SampleAttributes(entity, random);
        }
        /// <summary>
        /// Samples a new sensor based on these constraints.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled sensor.</returns>
        public override Entity Sample(string name,IUniformRandom random)
        {
            var entity = new Entity();

            entity.AddComponent(new Sensor(name));

            return SampleAttributes(entity, random);
        }
        #endregion
    }
}
