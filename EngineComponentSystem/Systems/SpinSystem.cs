using Engine.ComponentSystem.Components;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Applies a component's spin to its transform.
    /// </summary>
    public sealed class SpinSystem : AbstractComponentSystem<Spin>
    {
        protected override void UpdateComponent(GameTime gameTime, long frame, Spin component)
        {
            component.Entity.GetComponent<Transform>().AddRotation(component.Value);
        }
    }
}
