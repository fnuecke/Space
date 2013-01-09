using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Systems;

namespace Engine.ComponentSystem.Common.Systems
{
    /// <summary>Applies a component's velocity to its transform.</summary>
    public sealed class VelocitySystem : AbstractParallelComponentSystem<Velocity>
    {
        /// <summary>Updates the component by applying its velocity to its transform.</summary>
        /// <param name="frame">The current simulation frame.</param>
        /// <param name="component">The component to update.</param>
        protected override void UpdateComponent(long frame, Velocity component)
        {
            ((Transform) Manager.GetComponent(component.Entity, Transform.TypeId)).AddTranslation(ref component.Value);
        }
    }
}