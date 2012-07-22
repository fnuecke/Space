using Engine.ComponentSystem.Components;

namespace Engine.ComponentSystem.Systems
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
