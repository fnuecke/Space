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
        
        /// <summary>Gets the accumulated force currently being applied to the body, which will be applied in the next update.</summary>
        Vector2 Force { get; }

        /// <summary>Applies a force onto the component's center of mass.</summary>
        void ApplyForceToCenter(Vector2 force);
    }
}
