﻿using Engine.ComponentSystem.Entities;
using Engine.Util;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Constraints
{
    /// <summary>
    /// Constraints for generating thrusters.
    /// </summary>
    public sealed class ThrusterConstraints : ItemConstraints
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

            entity.AddComponent(new Thruster());

            return SampleAttributes(entity, random);
        }
        /// <summary>
        /// Samples a new thruster based on these constraints.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled thruster.</returns>
        public override Entity Sample(string name,IUniformRandom random)
        {
            var entity = new Entity();

            entity.AddComponent(new Thruster(name));

            return SampleAttributes(entity, random);
        }
        #endregion
    }
}
