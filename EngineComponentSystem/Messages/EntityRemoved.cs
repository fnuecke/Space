namespace Engine.ComponentSystem.Messages
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
    }
}
