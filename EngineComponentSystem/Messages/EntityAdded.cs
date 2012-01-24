using Engine.ComponentSystem.Entities;

namespace Engine.ComponentSystem.Messages
{
    /// <summary>
    /// Sent by entity managers when a new entity was added.
    /// </summary>
    public struct EntityAdded
    {
        /// <summary>
        /// The entity that was added.
        /// </summary>
        public Entity Entity;
    }
}
