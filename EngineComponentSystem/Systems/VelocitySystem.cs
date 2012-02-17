using Engine.ComponentSystem.Components;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Applies a component's velocity to its transform.
    /// </summary>
    public sealed class VelocitySystem : AbstractComponentSystem<Velocity>
    {
        public override void UpdateComponent(GameTime gameTime, long frame, Velocity component)
        {
            component.Entity.GetComponent<Transform>().AddTranslation(ref component.Value);
        }
    }
}
