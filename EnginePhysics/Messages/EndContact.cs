namespace Engine.Physics.Messages
{
    /// <summary>
    /// Used to indicate a collision ended.
    /// </summary>
    public struct EndContact
    {
        /// <summary>
        /// A unique ID for a contact, which allows associating
        /// begin and end events.
        /// </summary>
        public int ContactId;

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
