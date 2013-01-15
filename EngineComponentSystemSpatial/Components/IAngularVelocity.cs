using Engine.ComponentSystem.Components;

namespace Engine.ComponentSystem.Spatial.Components
{
    /// <summary>
    /// Interface for components with rotational movement.
    /// </summary>
    public interface IAngularVelocity : IComponent
    {
        /// <summary>Gets the current rotation speed.</summary>
        float Value { get; }
    }
}
