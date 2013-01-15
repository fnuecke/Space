using Engine.ComponentSystem.Components;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Spatial.Components
{
    /// <summary>Interface for components implementing linear velocity.</summary>
    public interface IVelocity : IComponent
    {
        /// <summary>Gets the current linear velocity of the entity.</summary>
        Vector2 LinearVelocity { get; set; }
        
        /// <summary>Gets the current angular velocity of the entity.</summary>
        float AngularVelocity { get; set; }
    }
}
