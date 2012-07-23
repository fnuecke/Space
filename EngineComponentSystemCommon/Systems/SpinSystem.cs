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
            Manager.GetComponent<Transform>(component.Entity).AddRotation(component.Value);
        }
    }
}
