namespace Engine.ComponentSystem.Common.Messages
{
    /// <summary>
    /// Used to indicate a collision occurred.
    /// </summary>
    public struct Collision
    {
        /// <summary>
        /// The first entity that was involved in the collision.
        /// </summary>
        public int FirstEntity;

        /// <summary>
        /// The second entity that was involved in the collision.
        /// </summary>
        public int SecondEntity;
    }
}
