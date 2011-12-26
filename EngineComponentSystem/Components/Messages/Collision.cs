using Engine.ComponentSystem.Entities;

namespace Engine.ComponentSystem.Components.Messages
{
    public struct Collision
    {
        /// <summary>
        /// The other entity that was involved in the collision, i.e. the one
        /// we collided with.
        /// </summary>
        public IEntity OtherEntity;

        public static Collision Create(IEntity otherEntity)
        {
            Collision result;
            result.OtherEntity = otherEntity;
            return result;
        }
    }
}
