namespace Engine.ComponentSystem.Messages
{
    /// <summary>Sent by the <see cref="Manager"/> when an entity was removed.</summary>
    public struct EntityRemoved
    {
        /// <summary>The entity that was removed.</summary>
        public int Entity;
    }
}