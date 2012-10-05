using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Common.Messages
{
    /// <summary>
    /// Used to indicate a collision occurred.
    /// </summary>
    public struct BeginCollision
    {
        /// <summary>
        /// The first entity that was involved in the collision.
        /// </summary>
        public int EntityA;

        /// <summary>
        /// The second entity that was involved in the collision.
        /// </summary>
        public int EntityB;

        /// <summary>
        /// The normal giving the direction pointing from EntityA
        /// to EntityB at the time the collision occurred.
        /// </summary>
        public Vector2 Normal;
    }
}
