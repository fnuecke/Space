using Engine.ComponentSystem.Components;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Applies a component's velocity to its transform.
    /// </summary>
    public sealed class VelocitySystem : AbstractComponentSystem<Velocity>
    {
        protected override void UpdateComponent(long frame, Velocity component)
        {
            Manager.GetComponent<Transform>(component.Entity).AddTranslation(ref component.Value);
        }
    }
}
