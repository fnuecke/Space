using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Systems;

namespace Engine.ComponentSystem.Common.Systems
{
    /// <summary>
    /// Applies a component's spin to its transform.
    /// </summary>
    public sealed class SpinSystem : AbstractParallelComponentSystem<Spin>
    {
        /// <summary>
        /// Updates the component's entity by adjusting it's rotation.
        /// </summary>
        /// <param name="frame">The current frame.</param>
        /// <param name="component">The component.</param>
        protected override void UpdateComponent(long frame, Spin component)
        {
            ((Transform)Manager.GetComponent(component.Entity, Transform.TypeId)).AddRotation(component.Value);
        }
    }
}
