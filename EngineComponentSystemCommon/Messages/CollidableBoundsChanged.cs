using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Common.Messages
{
    /// <summary>
    /// Sent by <code>Collidable</code> instances when their bounds change.
    /// </summary>
    public struct CollidableBoundsChanged
    {
        /// <summary>
        /// The entity to which the collidable belongs.
        /// </summary>
        public int Entity;

        /// <summary>
        /// The new bounds of the collidable.
        /// </summary>
        public Rectangle Bounds;
    }
}
