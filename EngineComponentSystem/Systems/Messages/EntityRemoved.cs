using Engine.ComponentSystem.Entities;

namespace Engine.ComponentSystem.Systems.Messages
{
    /// <summary>
    /// Sent by entity managers when an entity was removed.
    /// </summary>
    public struct EntityRemoved
    {
        /// <summary>
        /// The entity that was removed.
        /// </summary>
        public Entity Entity;

        /// <summary>
        /// The UID the entity had, prior to removal.
        /// </summary>
        public int EntityUid;
    }
}
