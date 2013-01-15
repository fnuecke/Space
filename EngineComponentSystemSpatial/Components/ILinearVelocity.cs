using Engine.ComponentSystem.Components;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Spatial.Components
{
    /// <summary>Interface for components implementing linear velocity.</summary>
    public interface ILinearVelocity : IComponent
    {
        /// <summary>Gets the current linear velocity of the component.</summary>
        Vector2 Value { get; }
    }
}
