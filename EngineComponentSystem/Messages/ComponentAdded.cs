using Engine.ComponentSystem.Components;

namespace Engine.ComponentSystem.Messages
{
    /// <summary>Sent by the <see cref="Manager"/> when a component was added.</summary>
    public struct ComponentAdded
    {
        /// <summary>The component that was added.</summary>
        public IComponent Component;
    }
}