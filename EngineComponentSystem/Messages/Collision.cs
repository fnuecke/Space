using Engine.ComponentSystem.Entities;

namespace Engine.ComponentSystem.Components.Messages
{
    /// <summary>
    /// Used to indicate a collision occurred. Only sent to collidable objects,
    /// i.e. subclasses of <c>AbstractCollidable</c>, by the CollisionSystem.
    /// </summary>
    public struct Collision
    {
        /// <summary>
        /// The other entity that was involved in the collision, i.e. the one
        /// we collided with.
        /// </summary>
        public Entity OtherEntity;
    }
}
