using Engine.ComponentSystem.Components;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Applies an entity's acceleration to its velocity.
    /// </summary>
    public sealed class AccelerationSystem : AbstractComponentSystem<Acceleration>
    {
        protected override void UpdateComponent(GameTime gameTime, long frame, Acceleration component)
        {
            // Apply acceleration if velocity is available.
            Manager.GetComponent<Velocity>(component.Entity).Value += component.Value;
        }
    }
}
