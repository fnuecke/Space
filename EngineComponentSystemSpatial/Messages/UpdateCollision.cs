using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Spatial.Messages
{
    public struct UpdateCollision
    {
        /// <summary>A unique ID for a contact, which allows associating begin and end events.</summary>
        public int ContactId;

        /// <summary>The first entity that was involved in the collision.</summary>
        public int EntityA;

        /// <summary>The second entity that was involved in the collision.</summary>
        public int EntityB;

        /// <summary>The normal giving the direction pointing from EntityA to EntityB at the time the collision occurred.</summary>
        public Vector2 Normal;
    }
}