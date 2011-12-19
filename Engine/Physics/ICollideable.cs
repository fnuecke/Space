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
        bool Intersects(FPoint extents, FPoint previousPosition, FPoint position);

        /// <summary>
        /// Test for a collision with a moving sphere.
        /// </summary>
        /// <param name="radius">the radius of the sphere.</param>
        /// <param name="previousPosition">position of the sphere in the previous frame.</param>
        /// <param name="position">position of the sphere in the current frame.</param>
        /// <returns><code>true</code> if the objects collide.</returns>
        bool Intersects(Fixed radius, FPoint previousPosition, FPoint position);
    }
}
