using System;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Parameterizations;
using Space.ComponentSystem.Entities;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Represents a final death, i.e. when health reaches zero the entity will
    /// be removed from the simulation.
    /// </summary>
    public sealed class Death : AbstractComponent
    {
        #region Logic

        /// <summary>
        /// Checks if health is zero, and if so removes the entity from the
        /// simulation.
        /// </summary>
        /// <param name="parameterization">Logic parameterization.</param>
        public override void Update(object parameterization)
        {
            var health = Entity.GetComponent<Health>();

            if (health != null && health.Value == 0)
            {
                Entity.Manager.AddEntity(EntityFactory.CreateExplosion(Entity.GetComponent<Transform>().Translation));
                Entity.Manager.RemoveEntity(Entity);
            }
        }

        /// <summary>
        /// Accepts <c>DefaultLogicParameterization</c>.
        /// </summary>
        /// <param name="parameterizationType">The parameterization to check.</param>
        /// <returns>Whether its supported or not.</returns>
        public override bool SupportsUpdateParameterization(Type parameterizationType)
        {
            return parameterizationType == typeof(DefaultLogicParameterization);
        }

        #endregion
    }
}
