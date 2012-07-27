using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Systems;

namespace Engine.ComponentSystem.Common.Systems
{
    /// <summary>
    /// Applies a component's spin to its transform.
    /// </summary>
    public sealed class SpinSystem : AbstractComponentSystem<Spin>
    {
        protected override void UpdateComponent(long frame, Spin component)
        {
            ((Transform)Manager.GetComponent(component.Entity, Transform.TypeId)).AddRotation(component.Value);
        }
    }
}
