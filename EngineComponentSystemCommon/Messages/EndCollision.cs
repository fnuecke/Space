namespace Engine.ComponentSystem.Common.Messages
{
    /// <summary>
    /// Used to indicate a collision ended.
    /// </summary>
    public struct EndCollision
    {
        /// <summary>
        /// The first entity that was involved in the collision.
        /// </summary>
        public int EntityA;

        /// <summary>
        /// The second entity that was involved in the collision.
        /// </summary>
        public int EntityB;
    }
}
