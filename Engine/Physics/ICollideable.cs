using Engine.Math;

namespace Engine.Physics
{
    /// <summary>
    /// Interface for objects which support collision queries.
    /// </summary>
    public interface ICollideable
    {
        /// <summary>
        /// Test for a collision with a moving AABB.
        /// </summary>
        /// <param name="extents">size of the AABB.</param>
        /// <param name="previousPosition">position of the AABB in the previous frame.</param>
        /// <param name="position">position of the AABB in the current frame.</param>
        /// <returns><code>true</code> if the objects collide.</returns>
        bool Intersects(ref FPoint extents, ref FPoint previousPosition, ref FPoint position);

        /// <summary>
        /// Test for a collision with a moving sphere.
        /// </summary>
        /// <param name="radius">the radius of the sphere.</param>
        /// <param name="previousPosition">position of the sphere in the previous frame.</param>
        /// <param name="position">position of the sphere in the current frame.</param>
        /// <returns><code>true</code> if the objects collide.</returns>
        bool Intersects(Fixed radius, ref FPoint previousPosition, ref FPoint position);

        /// <summary>
        /// Notify the object that it collided with another object.
        /// 
        /// This can be useful to reduce the number of necessary collision
        /// checks. Another object that tested for collision with this object
        /// may (should) call this method if they collide. This object can then
        /// take appropriate action.
        /// </summary>
        void NotifyOfCollision();
    }
}
