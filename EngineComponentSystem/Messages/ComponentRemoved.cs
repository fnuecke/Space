using Engine.ComponentSystem.Components;

namespace Engine.ComponentSystem.Messages
{
    /// <summary>Sent by the <see cref="Manager"/> when a component was removed.</summary>
    public struct ComponentRemoved
    {
        /// <summary>The component that was removed.</summary>
        public IComponent Component;
    }
}
