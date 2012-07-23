using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Systems;

namespace Engine.ComponentSystem.Common.Systems
{
    /// <summary>
    /// Applies an entity's acceleration to its velocity.
    /// </summary>
    public sealed class AccelerationSystem : AbstractComponentSystem<Acceleration>
    {
        protected override void UpdateComponent(long frame, Acceleration component)
        {
            // Apply acceleration if velocity is available.
            Manager.GetComponent<Velocity>(component.Entity).Value += component.Value;
        }
    }
}
