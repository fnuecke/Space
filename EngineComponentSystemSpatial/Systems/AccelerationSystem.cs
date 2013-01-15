using Engine.ComponentSystem.Spatial.Components;
using Engine.ComponentSystem.Systems;

namespace Engine.ComponentSystem.Spatial.Systems
{
    /// <summary>Applies an entity's acceleration to its velocity.</summary>
    public sealed class AccelerationSystem : AbstractParallelComponentSystem<Acceleration>
    {
        #region Logic

        /// <summary>
        ///     Updates the component by applying its acceleration to its velocity. The acceleration is then reset to zero, to
        ///     allow an accumulative setting of it (e.g., engine + gravitation).
        /// </summary>
        /// <param name="frame">The current simulation frame.</param>
        /// <param name="component">The component to update.</param>
        protected override void UpdateComponent(long frame, Acceleration component)
        {
            // Apply acceleration if velocity is available.
            ((Velocity) Manager.GetComponent(component.Entity, Velocity.TypeId)).Value += component.Value;

            // Then reset it for the next update.
            component.Value.X = 0;
            component.Value.Y = 0;
        }

        #endregion
    }
}