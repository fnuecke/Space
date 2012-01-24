using Engine.ComponentSystem.Components;

namespace Engine.ComponentSystem.Messages
{
    /// <summary>
    /// Sent by entities to notify others that a new component has been added.
    /// </summary>
    public struct ComponentRemoved
    {
        /// <summary>
        /// The component that was removed.
        /// </summary>
        public AbstractComponent Component;
    }
}
