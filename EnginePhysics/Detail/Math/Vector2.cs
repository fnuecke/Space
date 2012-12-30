using Microsoft.Xna.Framework;

namespace Engine.Physics.Detail.Math
{
    /// <summary>
    /// Implements cross product for 2D-Vectors.
    /// </summary>
    internal static class Vector2Util
    {
        /// <summary>
        /// Perform the cross product on two vectors. In 2D this produces a scalar.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>The cross product of the two.</returns>
        public static float Cross(Vector2 a, Vector2 b)
        {
            return a.X * b.Y - a.Y * b.X;
        }

        /// <summary>
        /// Perform the cross product on a vector and a scalar. In 2D this produces
        /// a vector.
        /// </summary>
        /// <param name="v">The vector.</param>
        /// <param name="s">The scalar.</param>
        /// <returns>The cross product of the two.</returns>
        public static Vector2 Cross(Vector2 v, float s)
        {
            Vector2 result;
            result.X = s * v.Y;
            result.Y = -s * v.X;
            return result;
        }

        /// <summary>
        /// Perform the cross product on a scalar and a vector. In 2D this produces
        /// a vector.
        /// </summary>
        /// <param name="s">The scalar.</param>
        /// <param name="v">The vector.</param>
        /// <returns>The cross product of the two.</returns>
        public static Vector2 Cross(float s, Vector2 v)
        {
            Vector2 result;
            result.X = -s * v.Y;
            result.Y = s * v.X;
            return result;
        }
    }
}
